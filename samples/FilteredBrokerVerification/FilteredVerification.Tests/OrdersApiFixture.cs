using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Xunit.Abstractions;

namespace FilteredVerification.Tests;

public record Order(int Id, string Status);

/// <summary>
/// A tiny in-process "OrdersApi" provider. Tests arrange data directly in <see cref="Orders"/>
/// (the forum user's pattern) or via the /provider-states endpoint (the recommended pattern).
/// </summary>
public sealed class OrdersApiFixture : IAsyncLifetime
{
    public static readonly Uri ProviderUri = new("http://localhost:9223");

    public ConcurrentDictionary<int, Order> Orders { get; } = new();

    private WebApplication? app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(ProviderUri.ToString());
        this.app = builder.Build();

        this.app.MapGet("/orders/{id:int}", (int id) =>
        {
            if (!this.Orders.TryGetValue(id, out var order))
            {
                return Results.NotFound();
            }

            // SIMULATE_BUG=order2 -> provider regression on order 2's response body
            if (id == 2 && Environment.GetEnvironmentVariable("SIMULATE_BUG") == "order2")
            {
                return Results.Json(new { id, state = order.Status });
            }

            return Results.Json(new { id = order.Id, status = order.Status });
        });

        // Provider state endpoint used by the recommended single-verification approach
        this.app.MapPost("/provider-states", async (HttpRequest request) =>
        {
            using var doc = await JsonDocument.ParseAsync(request.Body);
            var state = doc.RootElement.GetProperty("state").GetString();
            this.Orders.Clear();
            switch (state)
            {
                case "order 1 exists": this.Orders[1] = new Order(1, "Pending"); break;
                case "order 2 exists": this.Orders[2] = new Order(2, "Fulfilling"); break;
                case "no orders exist": break;
            }
            return Results.Ok();
        });

        await this.app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (this.app != null)
        {
            await this.app.StopAsync();
            await this.app.DisposeAsync();
        }
    }
}

public static class Env
{
    public static readonly Uri BrokerUri = new(Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL") ?? "http://localhost:9292");
    public static readonly string ProviderVersion = Environment.GetEnvironmentVariable("PROVIDER_VERSION") ?? "local-dev";
    public static readonly string ProviderBranch = Environment.GetEnvironmentVariable("PROVIDER_BRANCH") ?? "main";

    public static PactVerifierConfig Config(ITestOutputHelper output) => new()
    {
        Outputters = new[] { new XunitOutput(output) },
        LogLevel = Enum.TryParse<PactLogLevel>(Environment.GetEnvironmentVariable("PACT_LOG_LEVEL"), true, out var level)
            ? level
            : PactLogLevel.Information,
    };

    private sealed class XunitOutput(ITestOutputHelper output) : IOutput
    {
        public void WriteLine(string line) => output.WriteLine(line);
    }
}
