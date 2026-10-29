using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PactNet.Exceptions;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;

namespace PactNet.Compatibility.Tests.Support
{
    /// <summary>
    /// The PactNet DSL routes for the plugin scenarios: synchronous plugin interactions
    /// (<c>WithSynchronousPluginInteractions</c>) and provider verification (<see cref="PactVerifier"/>)
    /// </summary>
    internal sealed class PluginDsl : IAsyncDisposable
    {
        private readonly CapturedOutput output = new();
        private IPactBuilder builder;
        private IDisposable owner;
        private TaskCompletionSource<int> started;
        private TaskCompletionSource done;
        private Task verify;

        /// <summary>Plugin contents as the DSL takes them; "fixture:" paths must be replaced before deserialising</summary>
        public static Dictionary<string, object> Contents(string contents) =>
            JsonSerializer.Deserialize<Dictionary<string, object>>(PluginWorld.PluginContents(contents));

        /// <summary>Define one synchronous plugin interaction through the DSL</summary>
        public static PluginDsl Define(string pactDir, string plugin, string version, string transport,
                                       string description, string contentType, string contents)
        {
            var dsl = new PluginDsl();
            var config = new PactConfig
            {
                PactDir = pactDir,
                Outputters = new IOutput[] { dsl.output },
                LogLevel = PactLogLevel.Information
            };

            ISynchronousPluginPactBuilderV4 builder = Pact.V4(PluginWorld.Consumer, PluginWorld.Provider, config)
                                                          .WithSynchronousPluginInteractions(plugin, version, transport);
            builder.UponReceiving(description).WithContent(contentType, Contents(contents));
            dsl.builder = builder;
            dsl.owner = builder;
            return dsl;
        }

        /// <summary>Define HTTP plugin interactions (all for the same plugin) through the DSL</summary>
        public static PluginDsl DefineHttp(string pactDir, string plugin, string version, IEnumerable<PluginWorld.HttpDef> defs)
        {
            var dsl = new PluginDsl();
            IHttpPluginPactBuilderV4 builder = Pact.V4(PluginWorld.Consumer, PluginWorld.Provider, dsl.Config(pactDir))
                                                   .WithHttpPluginInteractions(plugin, version);
            dsl.builder = builder;
            dsl.owner = builder;

            foreach (PluginWorld.HttpDef def in defs)
            {
                IHttpPluginRequestBuilderV4 request = builder.UponReceiving(def.Description).WithRequest(new HttpMethod(def.Method), def.Path);
                if (def.Request is var (_, reqType, reqContents))
                {
                    request = request.WithContent(reqType, Contents(reqContents));
                }

                IHttpPluginResponseBuilderV4 response = request.WillRespondWithStatus(200);
                if (def.Response is var (_, resType, resContents))
                {
                    response.WithContent(resType, Contents(resContents));
                }
            }

            return dsl;
        }

        private PactConfig Config(string pactDir) => new()
        {
            PactDir = pactDir,
            Outputters = new IOutput[] { this.output },
            LogLevel = PactLogLevel.Information
        };

        /// <summary>Start the mock server (DSL Verify) and hold it open until <see cref="FinishAsync"/></summary>
        public async Task<int> StartAsync()
        {
            this.started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            this.verify = Task.Run(() => this.builder.VerifyAsync(async ctx =>
            {
                this.started.TrySetResult(ctx.MockServerUri.Port);
                await this.done.Task;
            }));

            Task first = await Task.WhenAny(this.started.Task, this.verify);
            if (first == this.verify)
            {
                await this.verify; // surfaces the start failure
            }

            return await this.started.Task;
        }

        /// <summary>End the consumer test: (matched, mismatches). The DSL writes the pact file when matched</summary>
        public async Task<(bool Matched, JsonNode Mismatches)> FinishAsync()
        {
            this.done.TrySetResult();
            try
            {
                await this.verify;
                return (true, new JsonArray());
            }
            catch (PactFailureException)
            {
                return (false, this.output.Mismatches());
            }
        }

        public async ValueTask DisposeAsync()
        {
            this.done?.TrySetResult();
            if (this.verify != null)
            {
                try
                {
                    await this.verify;
                }
                catch
                {
                    // already reported
                }
            }

            this.owner?.Dispose();
        }

        /// <summary>Verify an HTTP (or message-over-HTTP) provider with the PactNet verifier: (exit code, output)</summary>
        public static (int ExitCode, string Output) VerifyProvider(int port, string pactFile)
        {
            var output = new CapturedOutput();
            var config = new PactVerifierConfig
            {
                Outputters = new IOutput[] { output },
                LogLevel = PactLogLevel.Information
            };

            using var verifier = new PactVerifier(PluginWorld.Provider, config);
            int exit;
            try
            {
                verifier.WithHttpEndpoint(new Uri($"http://127.0.0.1:{port}/"))
                        .WithFileSource(new FileInfo(pactFile))
                        .Verify();
                exit = 0;
            }
            catch (PactVerificationFailedException)
            {
                exit = 1;
            }
            catch (PactFailureException)
            {
                exit = 2;
            }

            return (exit, output.ToString());
        }

        private sealed class CapturedOutput : IOutput
        {
            private readonly List<string> lines = new();

            public void WriteLine(string line)
            {
                lock (this.lines)
                {
                    this.lines.Add(line);
                }
            }

            /// <summary>The DSL writes "Verification mismatches:", a blank line, then the mismatches JSON</summary>
            public JsonNode Mismatches()
            {
                lock (this.lines)
                {
                    int i = this.lines.IndexOf("Verification mismatches:");
                    string json = i >= 0 ? this.lines.Skip(i + 1).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) : null;
                    return json == null ? new JsonArray() : JsonNode.Parse(json);
                }
            }

            public override string ToString()
            {
                lock (this.lines)
                {
                    return string.Join(Environment.NewLine, this.lines);
                }
            }
        }
    }
}
