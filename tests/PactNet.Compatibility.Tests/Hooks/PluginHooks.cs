using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PactNet.Compatibility.Tests.Support;
using Reqnroll;

namespace PactNet.Compatibility.Tests.Hooks
{
    /// <summary>
    /// V4 plugin scenarios: serialised (plugins are shared per-process in pact_ffi), torn down before the ratchet,
    /// and each one's DSL/FFI route recorded to plugin-routes.txt
    /// </summary>
    [Binding]
    public sealed class PluginHooks
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);

        [BeforeScenario("plugin")]
        public static async Task BeforePlugin(PluginWorld plugin, CompatWorld world, ScenarioContext scenario)
        {
            await Gate.WaitAsync();
            plugin.Active = true;

            // @wip plugin scenarios are reported as unsupported (skipped) by the ratchet
            if (scenario.ScenarioInfo.CombinedTags.Contains("wip"))
            {
                world.Run("@wip", () => throw new NotSupportedException("@wip"));
            }
        }

        [AfterScenario("plugin", Order = 10)]
        public static async Task AfterPlugin(PluginWorld plugin)
        {
            try
            {
                await plugin.ShutdownAsync();
            }
            finally
            {
                Gate.Release();
            }
        }

        [AfterScenario("plugin", Order = 50)]
        public static void RecordRoute(PluginWorld plugin, CompatWorld world, FeatureContext feature, ScenarioContext scenario)
        {
            bool failed = world.Failure != null || world.Unsupported != null || scenario.TestError != null ||
                          scenario.ScenarioExecutionStatus != ScenarioExecutionStatus.OK;
            string route = failed ? "fail" : plugin.Ops.Any(o => o.StartsWith("ffi:", StringComparison.Ordinal)) ? "FFI" : "DSL";
            string ops = string.Join(",", plugin.Ops.Distinct());
            PluginWorld.Routes[(feature.FeatureInfo.Title, scenario.ScenarioInfo.Title)] = (route, ops);
        }

        [AfterTestRun]
        public static void WriteRoutes()
        {
            if (PluginWorld.Routes.IsEmpty)
            {
                return;
            }

            var text = new StringBuilder();
            foreach (var feature in PluginWorld.Routes.GroupBy(r => r.Key.Feature).OrderBy(g => g.Key))
            {
                text.AppendLine($"{feature.Key}: DSL={feature.Count(r => r.Value.Route == "DSL")} " +
                                $"FFI={feature.Count(r => r.Value.Route == "FFI")} fail={feature.Count(r => r.Value.Route == "fail")}");
            }

            text.AppendLine();
            foreach (var r in PluginWorld.Routes.OrderBy(r => r.Key.Feature).ThenBy(r => r.Key.Scenario))
            {
                text.AppendLine($"{r.Value.Route}\t{r.Key.Feature}: {r.Key.Scenario}\t[{r.Value.Ops}]");
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "plugin-routes.txt"), text.ToString());
        }
    }
}
