using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using PactNet.Exceptions;
using PactNet.Infrastructure.Outputters;
using PactNet.Matchers;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PactNet.Tests
{
    /// <summary>
    /// Mock server matching of repeated request headers, following RFC 9110 §5.3 (a recipient may combine
    /// multiple field lines with the same name into one comma-separated field value).
    /// </summary>
    /// <remarks>
    /// The requests are written over a raw socket because <see cref="HttpClient"/> merges repeated headers
    /// into a single field line, so it can't send each value as its own field line.
    /// </remarks>
    public class RepeatedHeaderTests
    {
        private readonly ITestOutputHelper output;
        private readonly CapturingOutput captured = new();

        public RepeatedHeaderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task H1_RepeatedFieldLines_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H1");

            builder.UponReceiving("a request with repeated X-Request field lines")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "a")
                   .WithHeader("X-Request", "b")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: a", "X-Request: b");
        }

        [Fact]
        public async Task H2_SingleCombinedFieldLine_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H2");

            builder.UponReceiving("a request with combined X-Request field line")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "a")
                   .WithHeader("X-Request", "b")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: a, b");
        }

        [Fact]
        public async Task H3_RepeatedDateValuesContainingCommas_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H3");

            builder.UponReceiving("a request with repeated X-Sunset date field lines")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Sunset", "Sat, 01 Jan 2028 00:00:00 GMT")
                   .WithHeader("X-Sunset", "Sun, 02 Jan 2028 00:00:00 GMT")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder,
                                        "X-Sunset: Sat, 01 Jan 2028 00:00:00 GMT",
                                        "X-Sunset: Sun, 02 Jan 2028 00:00:00 GMT");
        }

        [Fact]
        public async Task H4_SingletonContentTypeCombined_DoesNotMatch()
        {
            IPactBuilderV3 builder = this.NewBuilder("H4");

            builder.UponReceiving("a request with combined Content-Type field line")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("Content-Type", "text/plain")
                   .WithHeader("Content-Type", "text/html")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            HttpStatusCode? status = null;

            Func<Task> verify = () => builder.VerifyAsync(async ctx =>
            {
                status = await SendRawAsync(ctx.MockServerUri, "Content-Type: text/plain, text/html");
            });

            var exception = await verify.Should().ThrowAsync<PactMockServerMismatchException>();
            this.DumpOutput();

            status.Should().NotBe(HttpStatusCode.OK);
            this.captured.Mismatches.Should().NotBeNullOrWhiteSpace().And.NotBe("[]");

            // the same mismatches and this mock server's logs are available on the exception, not just the outputters
            exception.Which.Mismatches.Should().Be(this.captured.Mismatches);
            exception.Which.Mismatches.Should().ContainEquivalentOf("content-type");
            exception.Which.MockServerLogs.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task H5_ListHeaderCombined_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H5");

            builder.UponReceiving("a request with combined Cache-Control field line")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("Cache-Control", "no-cache")
                   .WithHeader("Cache-Control", "no-store")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "Cache-Control: no-cache, no-store");
        }

        [Fact]
        public async Task H6a_MatcherPerIndex_MatchesWithoutDeprecationWarning()
        {
            IPactBuilderV3 builder = this.NewBuilder("H6a");

            builder.UponReceiving("a request with repeated X-Request field lines and regex matchers")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", Match.Regex("first-1", @"^first-\d+$"))
                   .WithHeader("X-Request", Match.Regex("first-2", @"^first-\d+$"))
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: first-42", "X-Request: first-99");

            this.captured.Logs.Should().NotContainEquivalentOf("deprecat");
        }

        [Fact]
        public async Task H6b_QuotedValueContainingComma_MatchesAndWarns()
        {
            IPactBuilderV3 builder = this.NewBuilder("H6b");

            builder.UponReceiving("a request with a quoted X-Request value containing a comma")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "\"x , y\"")
                   .WithHeader("X-Request", "z")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: \"x, y\"", "X-Request: z");

            this.captured.Logs.Should().Contain("WARN").And.Contain("RFC 9110 §5.3");
        }

        [Fact]
        public async Task H8a_RepeatedLinkFieldLines_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H8a");

            builder.UponReceiving("a request with repeated Link field lines")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("Link", "<https://a.example/x,y>; rel=\"a\"")
                   .WithHeader("Link", "<https://b.example/>; rel=\"b\"")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder,
                                        "Link: <https://a.example/x,y>; rel=\"a\"",
                                        "Link: <https://b.example/>; rel=\"b\"");
        }

        [Fact]
        public async Task H8b_CombinedLinkFieldLine_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("H8b");

            builder.UponReceiving("a request with combined Link field line")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("Link", "<https://a.example/x,y>; rel=\"a\"")
                   .WithHeader("Link", "<https://b.example/>; rel=\"b\"")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder,
                                        "Link: <https://a.example/x,y>; rel=\"a\", <https://b.example/>; rel=\"b\"");
        }

        /// <summary>
        /// The repeated custom header scenario from the original <see cref="PactExtensionsTests"/> expectations
        /// (pact-net PR 570 renamed it to Accept-Language to avoid a mock server 500 on older FFI versions)
        /// </summary>
        [Fact]
        public async Task RepeatedCustomHeader_SeparateFieldLines_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("X-Request-lines");

            builder.UponReceiving("a request with repeated custom header field lines")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "request1")
                   .WithHeader("X-Request", "request2")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: request1", "X-Request: request2");
        }

        /// <summary>
        /// As above, but sent via <see cref="HttpClient"/> which combines the values into one field line
        /// </summary>
        [Fact]
        public async Task RepeatedCustomHeader_HttpClient_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("X-Request-httpclient");

            builder.UponReceiving("a request with a repeated custom header from HttpClient")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "request1")
                   .WithHeader("X-Request", "request2")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await builder.VerifyAsync(async ctx =>
            {
                using var client = new HttpClient { BaseAddress = ctx.MockServerUri };
                client.DefaultRequestHeaders.Add("X-Request", new[] { "request1", "request2" });

                HttpResponseMessage response = await client.GetAsync("/headers");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            });
        }

        [Fact]
        public async Task MultipleValuesOverload_SeparateFieldLines_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("values-overload");

            builder.UponReceiving("a request with repeated X-Request field lines from the values overload")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", "request1", "request2")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: request1", "X-Request: request2");
        }

        [Fact]
        public async Task MultipleMatchersOverload_SeparateFieldLines_Matches()
        {
            IPactBuilderV3 builder = this.NewBuilder("matchers-overload");

            builder.UponReceiving("a request with repeated X-Request field lines from the matchers overload")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WithHeader("X-Request", Match.Regex("first-1", @"^first-\d+$"), Match.Type("second"))
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK);

            await this.ShouldMatchAsync(builder, "X-Request: first-42", "X-Request: anything");
        }

        [Fact]
        public async Task MultipleValuesOverload_Response_ReturnsEachValue()
        {
            IPactBuilderV3 builder = this.NewBuilder("response-values-overload");

            builder.UponReceiving("a request for a response with a repeated header")
                   .WithRequest(HttpMethod.Get, "/headers")
                   .WillRespond()
                   .WithStatus(HttpStatusCode.OK)
                   .WithHeader("X-Response", "response1", "response2");

            await builder.VerifyAsync(async ctx =>
            {
                using var client = new HttpClient { BaseAddress = ctx.MockServerUri };

                HttpResponseMessage response = await client.GetAsync("/headers");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Headers.GetValues("X-Response").Should().BeEquivalentTo(new[] { "response1", "response2" },
                                                                                  o => o.WithStrictOrdering());
            });
        }

        private IPactBuilderV3 NewBuilder(string id)
        {
            var config = new PactConfig
            {
                PactDir = Environment.CurrentDirectory,
                Outputters = new IOutput[] { new XunitOutput(this.output), this.captured }
            };

            string consumer = $"RepeatedHeaderTests-{id}-Consumer";
            string provider = "RepeatedHeaderTests-Provider";

            File.Delete(Path.Combine(config.PactDir, $"{consumer}-{provider}.json"));

            return Pact.V3(consumer, provider, config).WithHttpInteractions();
        }

        /// <summary>
        /// Send the raw field lines, then assert a 200 and no mismatches (VerifyAsync throws on any mismatch)
        /// </summary>
        private async Task ShouldMatchAsync(IPactBuilderV3 builder, params string[] fieldLines)
        {
            HttpStatusCode? status = null;

            Func<Task> verify = () => builder.VerifyAsync(async ctx =>
            {
                status = await SendRawAsync(ctx.MockServerUri, fieldLines);
            });

            try
            {
                await verify.Should().NotThrowAsync();
                status.Should().Be(HttpStatusCode.OK);
            }
            finally
            {
                this.DumpOutput();
            }
        }

        private void DumpOutput()
        {
            if (!string.IsNullOrWhiteSpace(this.captured.Mismatches))
            {
                this.output.WriteLine("Captured mismatches: " + this.captured.Mismatches);
            }
        }

        /// <summary>
        /// Write a GET /headers request with each header field line exactly as given
        /// </summary>
        private static async Task<HttpStatusCode> SendRawAsync(Uri baseUri, params string[] fieldLines)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(baseUri.Host, baseUri.Port);

            var request = new StringBuilder();
            request.Append("GET /headers HTTP/1.1\r\n");
            request.Append($"Host: {baseUri.Host}:{baseUri.Port}\r\n");

            foreach (string line in fieldLines)
            {
                request.Append(line).Append("\r\n");
            }

            request.Append("Connection: close\r\n\r\n");

            using NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(request.ToString());
            await stream.WriteAsync(bytes, 0, bytes.Length);

            using var reader = new StreamReader(stream, Encoding.ASCII);
            string statusLine = await reader.ReadLineAsync();

            // e.g. "HTTP/1.1 200 OK"
            string[] parts = statusLine?.Split(' ') ?? Array.Empty<string>();
            parts.Length.Should().BeGreaterThan(1, "the mock server should return a status line, got '{0}'", statusLine);

            await reader.ReadToEndAsync();

            return (HttpStatusCode)int.Parse(parts[1]);
        }

        /// <summary>
        /// Captures what <see cref="PactBuilder"/> writes to outputters, i.e. the mock server log buffer
        /// (which receives the FFI's log lines for this mock server) and any verification mismatches
        /// </summary>
        private class CapturingOutput : IOutput
        {
            private readonly List<string> lines = new();

            public string Logs => string.Join(Environment.NewLine, this.lines);

            public string Mismatches
            {
                get
                {
                    // PactBuilder writes the heading, a blank line, then the mismatches JSON
                    int index = this.lines.IndexOf("Verification mismatches:");
                    return index < 0 || index + 2 >= this.lines.Count ? null : this.lines[index + 2];
                }
            }

            public void WriteLine(string line)
            {
                lock (this.lines)
                {
                    this.lines.Add(line);
                }
            }
        }
    }
}
