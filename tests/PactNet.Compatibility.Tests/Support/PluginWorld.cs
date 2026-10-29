using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PactNet.Interop;

namespace PactNet.Compatibility.Tests.Support
{
    /// <summary>
    /// Per-scenario state for the V4 plugin features, plus a record of which scenarios went through the PactNet DSL
    /// and which needed direct FFI calls
    /// </summary>
    public sealed class PluginWorld
    {
        public const string Consumer = "c";
        public const string Provider = "p";

        /// <summary>(feature, scenario) -> (route, ops)</summary>
        internal static readonly ConcurrentDictionary<(string Feature, string Scenario), (string Route, string Ops)> Routes = new();

        private readonly List<string> ops = new();

        /// <summary>Set for scenarios tagged @plugin, so steps shared with the V1 features delegate here</summary>
        public bool Active { get; set; }

        internal string PactDir { get; } = Path.Combine(AppContext.BaseDirectory, "pacts", "plugins", Guid.NewGuid().ToString("N"));

        internal string PactFile => Path.Combine(this.PactDir, $"{Consumer}-{Provider}.json");

        internal Dictionary<string, string> InstalledVersions { get; } = new();

        // consumer side (FFI)
        internal PactHandle? Pact { get; set; }

        internal List<InteractionHandle> InteractionHandles { get; } = new();

        internal HashSet<string> LoadedPlugins { get; } = new();

        internal uint? LoadResult { get; set; }

        internal string LoadError { get; set; }

        internal int? MockServerPort { get; set; }

        internal bool? Matched { get; set; }

        internal JsonNode Mismatches { get; set; }

        internal JsonNode PactJson { get; set; }

        // client results
        internal (int Status, byte[] Body)? HttpResponse { get; set; }

        internal (int Status, byte[] Message)? GrpcResult { get; set; }

        internal string TcpReply { get; set; }

        // provider side
        internal List<string> PactFiles { get; } = new();

        internal int? ProviderPort { get; set; }

        internal List<IAsyncDisposable> Servers { get; } = new();

        internal int? VerifyExitCode { get; set; }

        internal string VerifyOutput { get; set; }

        internal JsonNode VerifyJson { get; set; }

        /// <summary>A step went through a direct FFI call PactNet has no public API for</summary>
        internal void Ffi(string op) => this.ops.Add("ffi:" + op);

        /// <summary>A step went through the PactNet DSL</summary>
        internal void Dsl(string op) => this.ops.Add("dsl:" + op);

        internal IReadOnlyList<string> Ops => this.ops;

        internal static string SuiteDir { get; } =
            Environment.GetEnvironmentVariable("PACT_COMPAT_SUITE") is { Length: > 0 } env
                ? env
                : typeof(PluginWorld).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                     .FirstOrDefault(a => a.Key == "PluginSuiteDir")?.Value;

        internal static string Fixture(string name) => Path.Combine(SuiteDir, "fixtures", name);

        /// <summary>A body cell: "file: X" reads the suite fixture, anything else is the literal text</summary>
        internal static byte[] BodyCell(string value) =>
            value.StartsWith("file: ", StringComparison.Ordinal)
                ? File.ReadAllBytes(Fixture(value.Substring("file: ".Length).Trim()))
                : Encoding.UTF8.GetBytes(value);

        /// <summary>Plugin contents may reference suite fixtures as "fixture:path"</summary>
        internal static string PluginContents(string contents) =>
            contents.Replace("\"fixture:", "\"" + Path.Combine(SuiteDir, "fixtures") + "/");

        internal static string PluginDir =>
            Environment.GetEnvironmentVariable("PACT_PLUGIN_DIR") is { Length: > 0 } dir
                ? dir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pact", "plugins");

        internal static bool PluginProcessRunning(string name)
        {
            using var p = Process.Start(new ProcessStartInfo("pgrep", new[] { "-f", $"{PluginDir}/{name}-" })
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        internal PactHandle RequirePact() => this.Pact ?? throw new InvalidOperationException("No interaction is being defined");

        internal InteractionHandle Current =>
            this.InteractionHandles.Count > 0 ? this.InteractionHandles[^1] : throw new InvalidOperationException("No interaction is being defined");

        internal JsonNode RequirePactJson() => this.PactJson ?? throw new InvalidOperationException("No pact file has been written");

        internal JsonNode FirstInteraction() => this.RequirePactJson()["interactions"]?[0] ?? throw new InvalidOperationException("The pact file has no interactions");

        /// <summary>Lazily create the consumer pact handle (V4)</summary>
        internal PactHandle EnsurePact()
        {
            if (this.Pact == null)
            {
                PactHandle pact = NativeInterop.NewPact(Consumer, Provider);
                NativeInterop.WithSpecification(pact, PactSpecification.V4);
                this.Pact = pact;
            }

            return this.Pact.Value;
        }

        internal void LoadPlugin(string name, string version)
        {
            this.Ffi("using_plugin");
            uint rc = PluginFfi.UsingPlugin(this.EnsurePact(), name, version);
            this.LoadResult = rc;
            if (rc == 0)
            {
                this.LoadedPlugins.Add(name);
            }
            else
            {
                this.LoadError = PluginFfi.LastError();
            }
        }

        internal void EnsurePlugin(string name)
        {
            if (!this.LoadedPlugins.Contains(name))
            {
                this.InstalledVersions.TryGetValue(name, out string version);
                this.LoadPlugin(name, version);
                CompatWorld.Check(this.LoadResult == 0, $"Loading plugin {name} {version} failed ({this.LoadResult}): {this.LoadError}");
            }
        }

        internal void ReadPactFile()
        {
            CompatWorld.Check(File.Exists(this.PactFile), $"Expected a pact file at {this.PactFile}");
            this.PactJson = JsonNode.Parse(File.ReadAllText(this.PactFile));
        }

        internal async Task ShutdownAsync()
        {
            foreach (IAsyncDisposable server in this.Servers)
            {
                try
                {
                    await server.DisposeAsync();
                }
                catch
                {
                    // best effort
                }
            }

            this.Servers.Clear();

            if (this.MockServerPort is int port)
            {
                NativeInterop.CleanupMockServer(port);
                this.MockServerPort = null;
            }

            if (this.Pact is PactHandle pact)
            {
                PluginFfi.CleanupPlugins(pact);
                PluginFfi.FreePactHandle(pact);
                this.Pact = null;
            }
        }
    }
}
