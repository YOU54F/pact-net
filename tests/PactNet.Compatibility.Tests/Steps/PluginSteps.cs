using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PactNet.Compatibility.Tests.Support;
using PactNet.Interop;
using Reqnroll;
using TableRow = Reqnroll.DataTableRow;

namespace PactNet.Compatibility.Tests.Steps
{
    /// <summary>
    /// Steps for features/V4/plugins (plugin_http, plugin_message, plugin_sync_message, plugin_transport, plugin_lifecycle)
    /// </summary>
    [Binding]
    public sealed class PluginSteps
    {
        private readonly CompatWorld world;
        private readonly PluginWorld plugin;

        public PluginSteps(CompatWorld world, PluginWorld plugin)
        {
            this.world = world;
            this.plugin = plugin;
        }

        private void Run(string step, Action body) => this.world.Run(step, () =>
        {
            this.RequireActive();
            body();
        });

        private Task RunAsync(string step, Func<Task> body) => this.world.RunAsync(step, () =>
        {
            this.RequireActive();
            return body();
        });

        private void RequireActive()
        {
            if (!this.plugin.Active)
            {
                throw new NotSupportedException("Plugin steps only run in @plugin scenarios");
            }
        }

        // ---------------------------------------------------------------- plugins

        [StepDefinition(@"^the ""([^""]*)"" plugin version ""([^""]*)"" is installed$")]
        public void PluginInstalled(string name, string version) => this.Run("plugin installed", () =>
        {
            string dir = Path.Combine(PluginWorld.PluginDir, $"{name}-{version}");
            if (!Directory.Exists(dir))
            {
                throw new NotSupportedException($"Plugin {name} {version} is not installed in {PluginWorld.PluginDir}");
            }

            this.plugin.InstalledVersions[name] = version;
        });

        [StepDefinition(@"^the ""([^""]*)"" plugin is not installed$")]
        public void PluginNotInstalled(string name) => this.Run("plugin not installed", () =>
        {
            bool installed = Directory.Exists(PluginWorld.PluginDir) &&
                             Directory.EnumerateDirectories(PluginWorld.PluginDir, name + "-*").Any();
            CompatWorld.Check(!installed, $"Plugin {name} is installed in {PluginWorld.PluginDir}");
        });

        [StepDefinition(@"^the ""([^""]*)"" plugin is loaded for the test$")]
        public void PluginLoadedAnyVersion(string name) => this.PluginLoaded(name, null);

        [StepDefinition(@"^the ""([^""]*)"" plugin version ""([^""]*)"" is loaded for the test$")]
        public void PluginLoaded(string name, string version) => this.Run("plugin loaded", () =>
            this.plugin.LoadPlugin(name, string.IsNullOrEmpty(version) ? null : version));

        [StepDefinition(@"^loading the plugin will fail with an error that mentions ""([^""]*)""$")]
        public void LoadFails(string text) => this.Run("plugin load failed", () =>
        {
            CompatWorld.Check(this.plugin.LoadResult is > 0, $"Expected loading the plugin to fail, got {this.plugin.LoadResult}");
            CompatWorld.Check(this.plugin.LoadError?.Contains(text) == true, $"Expected the error to mention '{text}', got '{this.plugin.LoadError}'");
        });

        [StepDefinition(@"^the test will not have panicked$")]
        public void NotPanicked() => this.Run("no panic", () =>
            CompatWorld.Check(this.plugin.LoadResult != 1, "The plugin load panicked"));

        [StepDefinition(@"^the plugins are cleaned up$")]
        public void PluginsCleanedUp() => this.Run("plugins cleaned up", () =>
        {
            this.plugin.Ffi("cleanup_plugins");
            PactHandle pact = this.plugin.RequirePact();
            PluginFfi.CleanupPlugins(pact);
            PluginFfi.FreePactHandle(pact);
            this.plugin.Pact = null;
        });

        [StepDefinition(@"^no ""([^""]*)"" plugin process will still be running$")]
        public Task NoPluginProcess(string name) => this.RunAsync("no plugin process", async () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (!PluginWorld.PluginProcessRunning(name))
                {
                    return;
                }

                await Task.Delay(200);
            }

            throw new CompatAssertionException($"A {name} plugin process is still running");
        });

        // ---------------------------------------------------------------- consumer: defining interactions

        [StepDefinition(@"^an HTTP interaction is being defined for a consumer test$")]
        [StepDefinition(@"^another HTTP interaction is being defined for the same consumer test$")]
        public void HttpInteraction() => this.Run("HTTP interaction", () =>
            this.plugin.HttpDefs.Add(new PluginWorld.HttpDef { Description = $"plugin interaction {this.plugin.HttpDefs.Count + 1}" }));

        [StepDefinition(@"^a message interaction is being defined for a consumer test$")]
        public void MessageInteraction() => this.Run("message interaction", () =>
        {
            this.plugin.Dsl("WithAsynchronousPluginInteractions");
            this.plugin.AsyncDefined = true;
        });

        [StepDefinition(@"^a synchronous message interaction is being defined for a consumer test$")]
        public void SyncMessageInteraction() => this.Run("synchronous message interaction", () =>
        {
            this.plugin.Dsl("WithSynchronousPluginInteractions");
            this.plugin.SyncDefined = true;
        });

        [StepDefinition(@"^the HTTP request is ""([^""]*)"" ""([^""]*)""$")]
        public void HttpRequest(string method, string path) => this.Run("HTTP request", () =>
        {
            this.plugin.CurrentHttp.Method = method;
            this.plugin.CurrentHttp.Path = path;
        });

        [StepDefinition(@"^the HTTP (request|response) body is set with the ""([^""]*)"" plugin for ""([^""]*)"" with:$")]
        public void HttpPluginBody(string part, string name, string contentType, string contents) => this.Run("HTTP plugin body", () =>
        {
            if (part == "request")
            {
                this.plugin.CurrentHttp.Request = (name, contentType, contents);
            }
            else
            {
                this.plugin.CurrentHttp.Response = (name, contentType, contents);
            }
        });

        [StepDefinition(@"^the (?:synchronous )?message contents are set with the ""([^""]*)"" plugin for ""([^""]*)"" with:$")]
        public void MessagePluginContents(string name, string contentType, string contents) => this.Run("message plugin contents", () =>
        {
            if (this.plugin.SyncDefined)
            {
                this.plugin.Dsl("WithContent");
                this.plugin.SyncContents = (name, contentType, contents);
                return;
            }

            if (this.plugin.AsyncDefined)
            {
                this.plugin.Dsl("WithContent");
                this.plugin.AsyncContents = (name, contentType, contents);
                return;
            }

            this.SetPluginContents(name, InteractionPart.Request, contentType, contents);
        });

        private const string SyncDescription = "plugin message 1";

        private PluginDsl DefineSync(string pactDir, string transport)
        {
            var (name, contentType, contents) = this.plugin.SyncContents ?? throw new InvalidOperationException("No synchronous message contents were set");
            return PluginDsl.Define(pactDir, name, this.plugin.PluginVersion(name), transport, SyncDescription, contentType, contents);
        }

        [StepDefinition(@"^the HTTP response body is ""([^""]*)"" with content type ""([^""]*)""$")]
        public void HttpResponseBody(string body, string contentType) => this.Run("HTTP response body", () =>
            this.plugin.CurrentHttp.PlainResponse = (body, contentType));

        /// <summary>
        /// The DSL (<c>WithHttpPluginInteractions</c>) takes one plugin per builder, needs a request line, and has no
        /// plain (non-plugin) body; it also only writes a pact through a passing mock server test
        /// </summary>
        private bool HttpDslCapable(bool mockServer) =>
            mockServer && this.plugin.Pact == null && this.plugin.HttpDefs.Count > 0 &&
            this.plugin.HttpDefs.All(d => d.Method != null && d.PlainResponse == null && d.Plugins.Any()) &&
            this.plugin.HttpDefs.SelectMany(d => d.Plugins).Distinct().Count() == 1;

        /// <summary>Build the recorded HTTP interactions through the FFI pact handle</summary>
        private void BuildHttpWithFfi()
        {
            foreach (PluginWorld.HttpDef def in this.plugin.HttpDefs)
            {
                this.plugin.Ffi("new_interaction");
                this.plugin.InteractionHandles.Add(NativeInterop.NewInteraction(this.plugin.EnsurePact(), def.Description));
                if (def.Method != null)
                {
                    this.plugin.Ffi("with_request");
                    NativeInterop.WithRequest(this.plugin.Current, def.Method, def.Path);
                }

                if (def.Request is var (reqPlugin, reqType, reqContents))
                {
                    this.SetPluginContents(reqPlugin, InteractionPart.Request, reqType, reqContents);
                }

                if (def.Response is var (resPlugin, resType, resContents))
                {
                    this.SetPluginContents(resPlugin, InteractionPart.Response, resType, resContents);
                }

                if (def.PlainResponse is var (body, contentType))
                {
                    this.plugin.Ffi("with_body");
                    NativeInterop.WithBody(this.plugin.Current, InteractionPart.Response, contentType, Encoding.UTF8.GetString(PluginWorld.BodyCell(body)));
                }
            }

            this.plugin.HttpDefs.Clear();
        }

        private void SetPluginContents(string name, InteractionPart part, string contentType, string contents)
        {
            this.plugin.EnsurePlugin(name);
            this.plugin.Ffi("interaction_contents");
            uint rc = PluginFfi.InteractionContents(this.plugin.Current, part, contentType, PluginWorld.PluginContents(contents));
            CompatWorld.Check(rc == 0, $"Setting the {name} plugin contents failed ({rc}): {PluginFfi.LastError()}");
        }

        // ---------------------------------------------------------------- consumer: pact file and mock servers

        [StepDefinition(@"^the Pact file for the test is generated$")]
        public void PactGenerated() => this.Run("pact file generated", () =>
        {
            if (this.plugin.SyncContents is var (name, contentType, contents))
            {
                // The DSL only writes a pact through a mock server, and writing a synchronous message pact from the
                // HTTP mock server panics in pact_ffi, so a pact with no transport mock server goes through the FFI
                this.plugin.Ffi("new_sync_message_interaction");
                this.plugin.InteractionHandles.Add(PluginFfi.NewSyncMessageInteraction(this.plugin.EnsurePact(), SyncDescription));
                this.SetPluginContents(name, InteractionPart.Request, contentType, contents);
            }

            if (this.plugin.AsyncContents is var (asyncName, asyncType, asyncContents))
            {
                this.plugin.Dsl("Verify");
                PluginDsl.WriteAsyncMessage(this.plugin.PactDir, asyncName, this.plugin.PluginVersion(asyncName), "plugin message 1", asyncType, asyncContents);
                this.plugin.ReadPactFile();
                return;
            }

            // the DSL only writes a pact through a passing mock server test
            this.BuildHttpWithFfi();

            this.plugin.Ffi("pact_handle_write_file");
            int rc = NativeInterop.WritePactFile(this.plugin.RequirePact(), this.plugin.PactDir, false);
            CompatWorld.Check(rc == 0, $"Writing the pact file failed ({rc})");
            this.plugin.ReadPactFile();
        });

        [StepDefinition(@"^the mock server is started with (?:the plugin interaction|all the interactions)$")]
        public void MockServerStarted() => this.Run("mock server started", () => this.StartMockServer("http"));

        [StepDefinition(@"^a mock server is started for the ""([^""]*)"" transport$")]
        public void MockServerForTransport(string transport) => this.Run("transport mock server started", () => this.StartMockServer(transport));

        private void StartMockServer(string transport)
        {
            if (this.plugin.SyncContents != null)
            {
                this.plugin.Dsl("VerifyAsync(" + transport + ")");
                this.plugin.MockDsl = this.DefineSync(this.plugin.PactDir, transport);
                this.plugin.MockServerPort = this.plugin.MockDsl.StartAsync().GetAwaiter().GetResult();
                return;
            }

            if (transport == "http" && this.HttpDslCapable(mockServer: true))
            {
                string name = this.plugin.HttpDefs.SelectMany(d => d.Plugins).First();
                this.plugin.Dsl("WithHttpPluginInteractions");
                this.plugin.MockDsl = PluginDsl.DefineHttp(this.plugin.PactDir, name, this.plugin.PluginVersion(name), this.plugin.HttpDefs);
                this.plugin.MockServerPort = this.plugin.MockDsl.StartAsync().GetAwaiter().GetResult();
                return;
            }

            this.BuildHttpWithFfi();

            this.plugin.Ffi("create_mock_server_for_transport");
            int port = NativeInterop.CreateMockServerForTransport(this.plugin.RequirePact(), "127.0.0.1", 0, transport, null);
            CompatWorld.Check(port > 0, $"Starting the {transport} mock server failed ({port}): {PluginFfi.LastError()}");
            this.plugin.MockServerPort = port;
        }

        private int RequireMockServer() => this.plugin.MockServerPort ?? throw new InvalidOperationException("The mock server has not been started");

        [StepDefinition(@"^a ""([^""]*)"" request is made to ""([^""]*)""$")]
        public Task RequestMadeNoBody(string method, string path) => this.RequestMade(method, path, null, null);

        [StepDefinition(@"^a ""([^""]*)"" request is made to ""([^""]*)"" with content type ""([^""]*)"" and body ""([^""]*)""$")]
        public Task RequestMade(string method, string path, string contentType, string body) => this.RunAsync("request made", async () =>
        {
            byte[] bytes = string.IsNullOrEmpty(body) ? null : PluginWorld.BodyCell(body);
            this.plugin.HttpResponse = await PluginProviders.HttpRequestAsync(this.RequireMockServer(), method, path,
                string.IsNullOrEmpty(contentType) ? null : contentType, bytes);
        });

        [StepDefinition(@"^the response body will be ""([^""]*)""$")]
        public void ResponseBody(string expected) => this.Run("response body", () =>
        {
            byte[] actual = (this.plugin.HttpResponse ?? throw new InvalidOperationException("No request was made")).Body;
            byte[] wanted = PluginWorld.BodyCell(expected);
            CompatWorld.Check(BytesOrJsonEqual(wanted, actual),
                $"Expected body '{Encoding.UTF8.GetString(wanted)}' but got '{Encoding.UTF8.GetString(actual)}'");
        });

        [StepDefinition(@"^the line ""([^""]*)"" is sent to the mock server over TCP$")]
        public Task TcpLineSent(string line) => this.RunAsync("TCP line sent", async () =>
            this.plugin.TcpReply = await PluginProviders.TcpLineAsync(this.RequireMockServer(), line));

        [StepDefinition(@"^the line ""([^""]*)"" will be received from the mock server$")]
        public void TcpLineReceived(string line) => this.Run("TCP line received", () =>
            CompatWorld.Check(this.plugin.TcpReply?.Trim() == line, $"Expected '{line}' but received '{this.plugin.TcpReply}'"));

        [StepDefinition(@"^the gRPC method ""([^""]*)"" is called on the mock server with an empty message$")]
        public Task GrpcCalledEmpty(string method) => this.GrpcCalled(method, null);

        [StepDefinition(@"^the gRPC method ""([^""]*)"" is called on the mock server with ""([^""]*)""$")]
        public Task GrpcCalled(string method, string message) => this.RunAsync("gRPC call", async () =>
        {
            byte[] bytes = string.IsNullOrEmpty(message) ? Array.Empty<byte>() : PluginWorld.BodyCell(message);
            this.plugin.GrpcResult = await PluginProviders.GrpcCallAsync(this.RequireMockServer(), method, bytes);
        });

        [StepDefinition(@"^the gRPC call will return ""([^""]*)""$")]
        public void GrpcReturns(string expected) => this.Run("gRPC result", () =>
        {
            var (status, message) = this.plugin.GrpcResult ?? throw new InvalidOperationException("No gRPC call was made");
            CompatWorld.Check(status == 0, $"Expected gRPC status OK but got {PluginProviders.GrpcStatusName(status)}");
            byte[] wanted = PluginWorld.BodyCell(expected);
            CompatWorld.Check(message != null && wanted.SequenceEqual(message),
                $"Expected gRPC message {Convert.ToHexString(wanted)} but got {(message == null ? "none" : Convert.ToHexString(message))}");
        });

        [StepDefinition(@"^the gRPC call will fail with status ""([^""]*)""$")]
        public void GrpcFails(string status) => this.Run("gRPC failure", () =>
        {
            var (actual, _) = this.plugin.GrpcResult ?? throw new InvalidOperationException("No gRPC call was made");
            CompatWorld.Check(PluginProviders.GrpcStatusName(actual) == status, $"Expected gRPC status {status} but got {PluginProviders.GrpcStatusName(actual)}");
        });

        /// <summary>Delegated from <see cref="HttpConsumerSteps"/> ("the pact test is done")</summary>
        internal void PactTestDone() => this.Run("pact test done", () =>
        {
            if (this.plugin.MockDsl != null)
            {
                var (matched, mismatches) = this.plugin.MockDsl.FinishAsync().GetAwaiter().GetResult();
                this.plugin.Matched = matched;
                this.plugin.Mismatches = mismatches;
                if (matched)
                {
                    this.plugin.ReadPactFile();
                }

                this.plugin.MockServerPort = null;
                return;
            }

            int port = this.RequireMockServer();
            this.plugin.Ffi("mock_server_matched");
            this.plugin.Matched = PluginFfi.MockServerMatched(port);
            IntPtr ptr = NativeInterop.MockServerMismatches(port);
            string json = ptr == IntPtr.Zero ? "[]" : Marshal.PtrToStringUTF8(ptr);
            this.plugin.Mismatches = JsonNode.Parse(json ?? "[]");

            if (this.plugin.Matched == true)
            {
                this.plugin.Ffi("write_pact_file");
                int rc = PluginFfi.WritePactFileForPort(port, this.plugin.PactDir, false);
                CompatWorld.Check(rc == 0, $"Writing the pact file failed ({rc})");
                this.plugin.ReadPactFile();
            }

            NativeInterop.CleanupMockServer(port);
            this.plugin.MockServerPort = null;
        });

        internal void StatusOk() => this.Run("status OK", () =>
            CompatWorld.Check(this.plugin.Matched == true, $"Expected the mock server to match, mismatches: {this.plugin.Mismatches?.ToJsonString()}"));

        internal void StatusMismatches() => this.Run("status mismatches", () =>
            CompatWorld.Check(this.plugin.Matched == false, "Expected mismatches, but the mock server matched"));

        internal void ResponseStatus(int status) => this.Run("response status", () =>
        {
            var (actual, body) = this.plugin.HttpResponse ?? throw new InvalidOperationException("No request was made");
            CompatWorld.Check(actual == status, $"Expected a {status} response but got {actual}: {Encoding.UTF8.GetString(body)}");
        });

        internal void PactWritten() => this.Run("pact file written", () =>
            CompatWorld.Check(this.plugin.PactJson != null && File.Exists(this.plugin.PactFile), $"Expected a pact file at {this.plugin.PactFile}"));

        [StepDefinition(@"^the mismatches will contain a mismatch with error ""(.*)""$")]
        public void MismatchError(string error) => this.Run("mismatch error", () =>
            CompatWorld.Check(AnyStringContains(this.plugin.Mismatches, error),
                $"Expected a mismatch containing '{error}', got {this.plugin.Mismatches?.ToJsonString()}"));

        // ---------------------------------------------------------------- consumer: pact file contents

        [StepDefinition(@"^the Pact file metadata will list the ""([^""]*)"" plugin with version ""([^""]*)""$")]
        public void MetadataPlugin(string name, string version) => this.Run("metadata plugin", () =>
        {
            JsonArray plugins = this.plugin.RequirePactJson()["metadata"]?["plugins"]?.AsArray();
            CompatWorld.Check(plugins?.Any(p => (string)p?["name"] == name && (string)p?["version"] == version) == true,
                $"Expected metadata.plugins to list {name} {version}, got {plugins?.ToJsonString()}");
        });

        [StepDefinition(@"^the first interaction in the pact file (request|response|contents) content type will be ""([^""]*)""$")]
        public void PartContentType(string part, string contentType) => this.Run("content type", () =>
        {
            JsonNode contents = PartContents(this.PartOf(part));
            string actual = (string)contents?["contentType"];
            CompatWorld.Check(actual == contentType, $"Expected {part} content type '{contentType}' but got '{actual}'");
        });

        [StepDefinition(@"^the first interaction in the pact file contents will be ""([^""]*)""$")]
        public void ContentsEqual(string expected) => this.PartContentsEqual("contents", expected);

        [StepDefinition(@"^the first interaction in the pact file (request|response) contents will be ""([^""]*)""$")]
        public void PartContentsEqual(string part, string expected) => this.Run("contents", () =>
        {
            JsonNode contents = PartContents(this.PartOf(string.IsNullOrEmpty(part) ? "contents" : part));
            string actual = (string)contents?["encoded"] is string enc && enc.StartsWith("base64", StringComparison.OrdinalIgnoreCase)
                ? Encoding.UTF8.GetString(Convert.FromBase64String((string)contents["content"]))
                : (string)contents?["content"];
            CompatWorld.Check(actual == expected, $"Expected contents '{expected}' but got '{actual}'");
        });

        [StepDefinition(@"^the first interaction (request|response|contents) in the pact file will have a matching rule for ""([^""]*)"" of type ""([^""]*)""$")]
        public void MatchingRule(string part, string key, string type) => this.Run("matching rule", () =>
        {
            JsonNode rules = this.PartOf(part)?["matchingRules"]?["body"]?[key]?["matchers"];
            CompatWorld.Check(rules is JsonArray arr && arr.Any(m => (string)m?["match"] == type),
                $"Expected a '{type}' matching rule for {key}, got {this.PartOf(part)?["matchingRules"]?.ToJsonString()}");
        });

        [StepDefinition(@"^the first interaction in the pact file will have plugin configuration for ""([^""]*)""$")]
        public void PluginConfiguration(string name) => this.Run("plugin configuration", () =>
            CompatWorld.Check(this.plugin.FirstInteraction()["pluginConfiguration"]?[name] != null,
                $"Expected plugin configuration for {name}, got {this.plugin.FirstInteraction()["pluginConfiguration"]?.ToJsonString()}"));

        [StepDefinition(@"^the second interaction in the pact file will not have plugin configuration$")]
        public void NoPluginConfiguration() => this.Run("no plugin configuration", () =>
        {
            JsonNode config = this.plugin.RequirePactJson()["interactions"]?[1]?["pluginConfiguration"];
            CompatWorld.Check(config == null || (config is JsonObject o && o.Count == 0), $"Expected no plugin configuration, got {config?.ToJsonString()}");
        });

        [StepDefinition(@"^the pact file will contain (\d+) interactions$")]
        public void InteractionCount(int count) => this.Run("interaction count", () =>
        {
            int actual = this.plugin.RequirePactJson()["interactions"]?.AsArray().Count ?? 0;
            CompatWorld.Check(actual == count, $"Expected {count} interactions but got {actual}");
        });

        [StepDefinition(@"^the first interaction in the Pact file will have a type of ""([^""]*)""$")]
        public void InteractionType(string type) => this.Run("interaction type", () =>
            CompatWorld.Check((string)this.plugin.FirstInteraction()["type"] == type, $"Expected type {type}, got {this.plugin.FirstInteraction()["type"]}"));

        [StepDefinition(@"^the first interaction in the pact file will contain (\d+) response messages?$")]
        public void ResponseMessages(int count) => this.Run("response messages", () =>
        {
            int actual = this.plugin.FirstInteraction()["response"] is JsonArray a ? a.Count : 0;
            CompatWorld.Check(actual == count, $"Expected {count} response messages but got {actual}");
        });

        [StepDefinition(@"^the first interaction in the Pact file will have ""([^""]*)"" = '(.*)'$")]
        public void InteractionField(string field, string json) => this.Run("interaction field", () =>
        {
            JsonNode actual = this.plugin.FirstInteraction()[field];
            JsonNode expected = JsonNode.Parse(json);
            if (field == "transport" && actual is JsonValue v && v.TryGetValue(out string s))
            {
                actual = JsonValue.Create(s.Split('/').Last());
            }

            CompatWorld.Check(JsonNode.DeepEquals(expected, actual), $"Expected {field} = {json} but got {actual?.ToJsonString()}");
        });

        private JsonNode PartOf(string part)
        {
            JsonNode interaction = this.plugin.FirstInteraction();
            return part switch
            {
                "contents" => interaction,
                "response" when interaction["response"] is JsonArray arr => arr[0],
                _ => interaction[part]
            };
        }

        private static JsonNode PartContents(JsonNode part) => part?["contents"] ?? part?["body"];

        // ---------------------------------------------------------------- provider: pact files

        [StepDefinition(@"^a Pact file with an HTTP ""([^""]*)"" ""([^""]*)"" interaction with plugin contents:$")]
        public void HttpPactFile(string method, string path, DataTable table) => this.Run("HTTP pact file", () =>
        {
            TableRow row = table.Rows[0];
            this.WritePactFile(row["plugin"], pact =>
            {
                this.plugin.Ffi("new_interaction");
                InteractionHandle i = NativeInterop.NewInteraction(pact, "plugin interaction");
                NativeInterop.WithRequest(i, method, path);
                foreach ((string column, InteractionPart part) in new[] { ("request", InteractionPart.Request), ("response", InteractionPart.Response) })
                {
                    if (row.TryGetValue(column, out string value) && !string.IsNullOrWhiteSpace(value))
                    {
                        this.plugin.Ffi("interaction_contents");
                        uint rc = PluginFfi.InteractionContents(i, part, row["content type"], PluginWorld.PluginContents(value));
                        CompatWorld.Check(rc == 0, $"Setting the {column} plugin contents failed ({rc}): {PluginFfi.LastError()}");
                    }
                }
            });
        });

        [StepDefinition(@"^a Pact file with a ""([^""]*)"" (message|synchronous message) interaction with plugin contents:$")]
        public void MessagePactFile(string description, string kind, DataTable table) => this.Run("message pact file", () =>
            this.WriteMessagePactFile(description, kind == "synchronous message", table.Rows[0], null));

        [StepDefinition(@"^a Pact file with a ""([^""]*)"" synchronous message interaction over the ""([^""]*)"" transport with plugin contents:$")]
        public void TransportPactFile(string description, string transport, DataTable table) => this.Run("transport pact file", () =>
            this.WriteMessagePactFile(description, true, table.Rows[0], transport));

        private void WriteMessagePactFile(string description, bool sync, TableRow row, string transport)
        {
            if (!sync)
            {
                string dir = Path.Combine(this.plugin.PactDir, "provider-" + this.plugin.PactFiles.Count);
                string name = row["plugin"];
                this.plugin.Dsl("WithAsynchronousPluginInteractions");
                PluginDsl.WriteAsyncMessage(dir, name, this.plugin.PluginVersion(name), description, row["content type"], row["contents"]);
                this.plugin.PactFiles.Add(Path.Combine(dir, $"{PluginWorld.Consumer}-{PluginWorld.Provider}.json"));
                return;
            }

            string file = this.WritePactFile(row["plugin"], pact =>
            {
                InteractionHandle i;
                if (sync)
                {
                    this.plugin.Ffi("new_sync_message_interaction");
                    i = PluginFfi.NewSyncMessageInteraction(pact, description);
                }
                else
                {
                    this.plugin.Ffi("new_message_interaction");
                    i = NativeInterop.NewMessageInteraction(pact, description);
                }

                this.plugin.Ffi("interaction_contents");
                uint rc = PluginFfi.InteractionContents(i, InteractionPart.Request, row["content type"], PluginWorld.PluginContents(row["contents"]));
                CompatWorld.Check(rc == 0, $"Setting the plugin contents failed ({rc}): {PluginFfi.LastError()}");
            });

            if (transport != null)
            {
                JsonNode json = JsonNode.Parse(File.ReadAllText(file));
                json["interactions"][0]["transport"] = transport;
                File.WriteAllText(file, json.ToJsonString());
            }
        }

        /// <summary>Build a pact with one plugin interaction on its own handle and write it out; returns the file</summary>
        private string WritePactFile(string pluginName, Action<PactHandle> define)
        {
            string dir = Path.Combine(this.plugin.PactDir, "provider-" + this.plugin.PactFiles.Count);
            PactHandle pact = NativeInterop.NewPact(PluginWorld.Consumer, PluginWorld.Provider);
            NativeInterop.WithSpecification(pact, PactSpecification.V4);
            try
            {
                this.plugin.Ffi("using_plugin");
                this.plugin.InstalledVersions.TryGetValue(pluginName, out string version);
                uint rc = PluginFfi.UsingPlugin(pact, pluginName, version);
                CompatWorld.Check(rc == 0, $"Loading plugin {pluginName} failed ({rc}): {PluginFfi.LastError()}");
                define(pact);
                this.plugin.Ffi("pact_handle_write_file");
                int written = NativeInterop.WritePactFile(pact, dir, false);
                CompatWorld.Check(written == 0, $"Writing the pact file failed ({written})");
            }
            finally
            {
                PluginFfi.CleanupPlugins(pact);
                PluginFfi.FreePactHandle(pact);
            }

            string file = Path.Combine(dir, $"{PluginWorld.Consumer}-{PluginWorld.Provider}.json");
            this.plugin.PactFiles.Add(file);
            return file;
        }

        // ---------------------------------------------------------------- provider: providers

        [StepDefinition(@"^a provider is started that returns ""([^""]*)"" with content type ""([^""]*)""$")]
        public Task HttpProvider(string body, string contentType) => this.RunAsync("HTTP provider", async () =>
            this.TrackProvider(await PluginProviders.StartBodyProviderAsync(PluginWorld.BodyCell(body), contentType)));

        [StepDefinition(@"^a provider is started that can generate the ""([^""]*)"" message with ""([^""]*)"" and content type ""([^""]*)""$")]
        public Task MessageProvider(string description, string body, string contentType) => this.RunAsync("message provider", async () =>
            this.TrackProvider(await PluginProviders.StartMessageProviderAsync(description, PluginWorld.BodyCell(body), contentType)));

        [StepDefinition(@"^a gRPC provider is started that answers ""([^""]*)"" with ""([^""]*)""$")]
        public Task GrpcProvider(string method, string message) => this.RunAsync("gRPC provider", async () =>
            this.TrackProvider(await PluginProviders.StartGrpcProviderAsync(method, PluginWorld.BodyCell(message))));

        [StepDefinition(@"^a TCP provider is started that replies to ""([^""]*)"" with ""([^""]*)""$")]
        public void TcpProvider(string expected, string reply) => this.Run("TCP provider", () =>
            this.TrackProvider(PluginProviders.StartTcpLineProvider(expected, reply)));

        [StepDefinition(@"^no provider is listening$")]
        public void NoProvider() => this.Run("no provider", () => this.plugin.ProviderPort = PluginProviders.FreePort());

        private void TrackProvider((int Port, IAsyncDisposable Server) started)
        {
            this.plugin.ProviderPort = started.Port;
            this.plugin.Servers.Add(started.Server);
        }

        // ---------------------------------------------------------------- provider: verification

        [StepDefinition(@"^the verification is run$")]
        public Task VerificationRunHttp() => this.VerificationRun(null);

        [StepDefinition(@"^the verification is run with the ""([^""]*)"" transport$")]
        public Task VerificationRun(string transport) => this.RunAsync("verification", async () =>
        {
            int port = this.plugin.ProviderPort ?? throw new InvalidOperationException("No provider has been started");
            if (string.IsNullOrEmpty(transport) && this.plugin.PactFiles.Count == 1)
            {
                // PactVerifier only has HTTP and message endpoints, so plugin transports (grpc, matt) stay on the FFI
                this.plugin.Dsl("PactVerifier");
                (this.plugin.VerifyExitCode, this.plugin.VerifyOutput) =
                    await Task.Run(() => PluginDsl.VerifyProvider(port, this.plugin.PactFiles[0]));
                this.plugin.VerifyJson = null;
                if (this.plugin.VerifyExitCode == 0)
                {
                    return;
                }

                // PactVerifier reports only pass/fail (no JSON results, and the outputters receive nothing on a
                // mismatch), so a failed verification is re-run through the FFI verifier for the mismatch details
            }

            this.plugin.Ffi("verifier");
            IntPtr handle = NativeInterop.VerifierNewForApplication("pact-net-compat", "0.0.0");
            try
            {
                NativeInterop.VerifierSetProviderInfo(handle, PluginWorld.Provider, "http", "127.0.0.1", (ushort)port, "/");
                if (!string.IsNullOrEmpty(transport))
                {
                    NativeInterop.AddProviderTransport(handle, transport, (ushort)port, "", transport == "grpc" ? "http" : "tcp");
                }

                foreach (string file in this.plugin.PactFiles)
                {
                    NativeInterop.VerifierAddFileSource(handle, file);
                }

                this.plugin.VerifyExitCode = await Task.Run(() => NativeInterop.VerifierExecute(handle));
                this.plugin.VerifyOutput = PluginFfi.TakeString(NativeInterop.VerifierOutput(handle, 1)) ?? string.Empty;
                string json = PluginFfi.TakeString(PluginFfi.VerifierJson(handle));
                this.plugin.VerifyJson = string.IsNullOrEmpty(json) ? null : JsonNode.Parse(json);
            }
            finally
            {
                NativeInterop.VerifierShutdown(handle);
            }
        });

        [StepDefinition(@"^the verification will be successful$")]
        public void VerificationSuccessful() => this.Run("verification successful", () =>
            CompatWorld.Check(this.plugin.VerifyExitCode == 0 && (bool?)this.plugin.VerifyJson?["result"] != false,
                $"Expected verification to pass (exit {this.plugin.VerifyExitCode}):\n{this.plugin.VerifyOutput}"));

        [StepDefinition(@"^the verification will NOT be successful$")]
        public void VerificationFailed() => this.Run("verification NOT successful", () =>
            CompatWorld.Check(this.plugin.VerifyExitCode is not null and not 0 && (bool?)this.plugin.VerifyJson?["result"] != true,
                $"Expected verification to fail (exit {this.plugin.VerifyExitCode}):\n{this.plugin.VerifyOutput}"));

        [StepDefinition(@"^the verification results will contain a ""([^""]*)"" error$")]
        public void VerificationError(string error) => this.Run("verification error", () =>
        {
            string[] types = error switch
            {
                "Body had differences" => new[] { "BodyMismatch", "BodyTypeMismatch" },
                "Metadata had differences" => new[] { "MetadataMismatch" },
                _ => new[] { error }
            };
            IEnumerable<string> found = (this.plugin.VerifyJson?["interactionResults"]?.AsArray() ?? new JsonArray())
                .SelectMany(r => r?["mismatch"]?["mismatches"]?.AsArray() ?? new JsonArray())
                .Select(m => (string)m?["type"]);
            CompatWorld.Check(found.Any(t => types.Contains(t)) || this.plugin.VerifyOutput.Contains(error),
                $"Expected a '{error}' error, results: {this.plugin.VerifyJson?.ToJsonString()}");
        });

        [StepDefinition(@"^the verification output will contain ""(.*)""$")]
        public void VerificationOutput(string text) => this.Run("verification output", () =>
            CompatWorld.Check(this.plugin.VerifyOutput?.Contains(text) == true, $"Expected the verification output to contain '{text}':\n{this.plugin.VerifyOutput}"));

        // ---------------------------------------------------------------- helpers

        private static bool BytesOrJsonEqual(byte[] expected, byte[] actual)
        {
            if (expected.SequenceEqual(actual))
            {
                return true;
            }

            try
            {
                return JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual));
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool AnyStringContains(JsonNode node, string text) => node switch
        {
            JsonObject o => o.Any(kv => AnyStringContains(kv.Value, text)),
            JsonArray a => a.Any(n => AnyStringContains(n, text)),
            JsonValue v when v.TryGetValue(out string s) => s.Contains(text),
            _ => false
        };
    }
}
