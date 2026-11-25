using PactNet.Verifier;
using Xunit.Abstractions;

namespace FilteredVerification.Tests;

/// <summary>
/// The recommended shape: ONE verifier run for the provider, no interaction filter, provider
/// states handled by a state-change endpoint. One broker round trip, and every published
/// verification result covers every interaction in the pact.
///
/// Filters (WithFilter / PACT_DESCRIPTION) are a local debugging aid - don't publish from them.
/// </summary>
public class SingleVerificationTests(OrdersApiFixture fixture, ITestOutputHelper output)
    : IClassFixture<OrdersApiFixture>
{
    [Fact]
    public void VerifyAllPacts()
    {
        _ = fixture;
        using var verifier = new PactVerifier("OrdersApi", Env.Config(output));

        verifier
            .WithHttpEndpoint(OrdersApiFixture.ProviderUri)
            .WithPactBrokerSource(Env.BrokerUri, options =>
            {
                options.ConsumerVersionSelectors(
                        new ConsumerVersionSelector { MainBranch = true },
                        new ConsumerVersionSelector { DeployedOrReleased = true })
                    .PublishResults(Env.ProviderVersion, publish => publish.ProviderBranch(Env.ProviderBranch));
            })
            .WithProviderStateUrl(new Uri(OrdersApiFixture.ProviderUri, "/provider-states"))
            .Verify();
    }
}
