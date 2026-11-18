using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PactNet.Drivers;
using PactNet.Interop;
using Xunit;
using Xunit.Abstractions;

namespace PactNet.Tests;

public class AvroSynchronousCapabilityTests(ITestOutputHelper output)
{
    private sealed class AvroCapabilityFactAttribute : FactAttribute
    {
        public AvroCapabilityFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("AVRO_SCHEMA_PATH") == null)
                this.Skip = "Set AVRO_SCHEMA_PATH and install Avro 0.1.3 to run the native capability probe.";
        }
    }

    [AvroCapabilityFact]
    public void Avro013_UnlabelledRecordDoesNotPopulateSynchronousParts()
    {
        string schema = Environment.GetEnvironmentVariable("AVRO_SCHEMA_PATH");
        string directory = Path.Combine(Path.GetTempPath(), "pact-avro-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        using var pact = new PactDriver().NewPluginPact("AvroSyncProbe", "AvroSyncProvider", "avro", "0.1.3", PactSpecification.V4);
        pact.NewSyncInteraction("an Avro request response").WithContent("avro/binary", new Dictionary<string, object>
        {
            ["pact:avro"] = schema,
            ["pact:record-name"] = "User",
            ["id"] = "matching(integer, 1)",
            ["username"] = "matching(equalTo, 'alice')"
        });
        pact.WritePactFile(directory);
        string json = File.ReadAllText(Path.Combine(directory, "AvroSyncProbe-AvroSyncProvider.json"));
        output.WriteLine(json);
        using var document = JsonDocument.Parse(json);
        var interaction = document.RootElement.GetProperty("interactions")[0];
        Assert.Equal("Synchronous/Messages", interaction.GetProperty("type").GetString());
        Assert.False(interaction.GetProperty("request").TryGetProperty("contents", out _));
        Assert.Equal(0, interaction.GetProperty("response").GetArrayLength());
    }
}