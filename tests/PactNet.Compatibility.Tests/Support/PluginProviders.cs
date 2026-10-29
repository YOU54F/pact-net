using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace PactNet.Compatibility.Tests.Support
{
    /// <summary>
    /// Throwaway providers (HTTP, message, gRPC, TCP line) and clients (HTTP, gRPC, TCP line) for the plugin scenarios
    /// </summary>
    internal static class PluginProviders
    {
        public static async Task<(int Port, IAsyncDisposable Server)> StartHttpAsync(RequestDelegate handler, bool http2 = false)
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0,
                l => l.Protocols = http2 ? HttpProtocols.Http2 : HttpProtocols.Http1));
            WebApplication app = builder.Build();
            app.Run(handler);
            await app.StartAsync();
            int port = new Uri(app.Urls.First()).Port;
            return (port, app);
        }

        /// <summary>A plain HTTP provider that always returns the body</summary>
        public static Task<(int Port, IAsyncDisposable Server)> StartBodyProviderAsync(byte[] body, string contentType) =>
            StartHttpAsync(async ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = contentType;
                await ctx.Response.Body.WriteAsync(body);
            });

        /// <summary>A message provider: the verifier POSTs the interaction description, the provider answers with the message</summary>
        public static Task<(int Port, IAsyncDisposable Server)> StartMessageProviderAsync(string description, byte[] body, string contentType) =>
            StartHttpAsync(async ctx =>
            {
                string requested = null;
                try
                {
                    using JsonDocument doc = await JsonDocument.ParseAsync(ctx.Request.Body);
                    requested = doc.RootElement.TryGetProperty("description", out JsonElement d) ? d.GetString() : null;
                }
                catch (JsonException)
                {
                }

                if (!string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) || requested != description)
                {
                    ctx.Response.StatusCode = 404;
                    return;
                }

                string metadata = Convert.ToBase64String(Encoding.UTF8.GetBytes(new JsonObject { ["contentType"] = contentType }.ToJsonString()));
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = contentType;
                ctx.Response.Headers["pact-message-metadata"] = metadata;
                await ctx.Response.Body.WriteAsync(body);
            });

        /// <summary>An h2c gRPC provider answering one method with a fixed message</summary>
        public static Task<(int Port, IAsyncDisposable Server)> StartGrpcProviderAsync(string method, byte[] message) =>
            StartHttpAsync(async ctx =>
            {
                using (var drain = new MemoryStream())
                {
                    await ctx.Request.Body.CopyToAsync(drain);
                }

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/grpc";
                if (ctx.Request.Path.Value == method)
                {
                    await ctx.Response.Body.WriteAsync(GrpcFrame(message));
                    ctx.Response.AppendTrailer("grpc-status", "0");
                }
                else
                {
                    ctx.Response.AppendTrailer("grpc-status", "12");
                }
            }, http2: true);

        /// <summary>A TCP provider replying to one line</summary>
        public static (int Port, IAsyncDisposable Server) StartTcpLineProvider(string expected, string reply)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync(cts.Token);
                    }
                    catch
                    {
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        using (client)
                        {
                            try
                            {
                                NetworkStream stream = client.GetStream();
                                using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
                                string line;
                                while ((line = await reader.ReadLineAsync(cts.Token)) != null)
                                {
                                    string answer = line.Trim() == expected ? reply : "MATTerrorMATT";
                                    byte[] bytes = Encoding.UTF8.GetBytes(answer + "\n");
                                    await stream.WriteAsync(bytes, cts.Token);
                                    await stream.FlushAsync(cts.Token);
                                }
                            }
                            catch
                            {
                                // connection dropped
                            }
                        }
                    });
                }
            });

            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return (port, new ActionDisposable(() =>
            {
                cts.Cancel();
                listener.Stop();
            }));
        }

        /// <summary>A port nothing listens on</summary>
        public static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static async Task<(int Status, byte[] Body)> HttpRequestAsync(int port, string method, string path, string contentType, byte[] body)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), $"http://127.0.0.1:{port}{path}");
            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
                if (contentType != null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }

            using HttpResponseMessage response = await client.SendAsync(request);
            return ((int)response.StatusCode, await response.Content.ReadAsByteArrayAsync());
        }

        /// <summary>Unary gRPC call over h2c. Returns the gRPC status and the response message (null when none)</summary>
        public static async Task<(int Status, byte[] Message)> GrpcCallAsync(int port, string method, byte[] message)
        {
            Exception last = null;
            foreach (string host in new[] { "127.0.0.1", "[::1]" })
            {
                try
                {
                    using var client = new HttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Post, $"http://{host}:{port}{method}")
                    {
                        Version = HttpVersion.Version20,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                        Content = new ByteArrayContent(GrpcFrame(message))
                    };
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
                    request.Headers.TryAddWithoutValidation("te", "trailers");

                    using HttpResponseMessage response = await client.SendAsync(request);
                    byte[] body = await response.Content.ReadAsByteArrayAsync();
                    string status = response.TrailingHeaders.TryGetValues("grpc-status", out var t) ? t.First()
                        : response.Headers.TryGetValues("grpc-status", out var h) ? h.First() : "0";
                    byte[] reply = body.Length >= 5 ? body.Skip(5).ToArray() : null;
                    return (int.Parse(status), reply);
                }
                catch (HttpRequestException e)
                {
                    last = e;
                }
            }

            throw last!;
        }

        /// <summary>Send one line over TCP and read one line back (null when nothing comes back)</summary>
        public static async Task<string> TcpLineAsync(int port, string line)
        {
            Exception last = null;
            foreach (IPAddress address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                using var client = new TcpClient(address.AddressFamily);
                try
                {
                    await client.ConnectAsync(address, port);
                }
                catch (SocketException e)
                {
                    last = e;
                    continue;
                }

                NetworkStream stream = client.GetStream();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"));
                await stream.FlushAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    return await reader.ReadLineAsync(cts.Token);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            throw last!;
        }

        public static string GrpcStatusName(int status) => status switch
        {
            0 => "OK",
            3 => "INVALID_ARGUMENT",
            5 => "NOT_FOUND",
            9 => "FAILED_PRECONDITION",
            12 => "UNIMPLEMENTED",
            13 => "INTERNAL",
            _ => status.ToString()
        };

        private static byte[] GrpcFrame(byte[] message)
        {
            var frame = new byte[5 + message.Length];
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1), (uint)message.Length);
            message.CopyTo(frame, 5);
            return frame;
        }

        private sealed class ActionDisposable : IAsyncDisposable
        {
            private readonly Action action;

            public ActionDisposable(Action action) => this.action = action;

            public ValueTask DisposeAsync()
            {
                this.action();
                return ValueTask.CompletedTask;
            }
        }
    }
}
