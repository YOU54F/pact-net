using PactNet.Verifier;
using Xunit.Abstractions;

namespace FilteredVerification.Tests;

/// <summary>
/// Mirrors the setup described in
/// https://community.smartbear.com/t/http-error-429-too-many-requests-to-pactflow/893
///
/// - one PactVerifier + PactVerifierSource per [Fact]
/// - every verifier pulls ALL pacts for the provider from the broker via consumer version selectors
/// - interactions are filtered by description, and the test arranges provider "state" itself
///   (description filter -> provider state mapping), rather than using a provider state endpoint
/// - verification results are published from every one of those runs
///
/// Each [Fact] is a full broker round trip (index, pacts-for-verification, fetch every pact,
/// publish a result per pact). N facts x M pacts multiplies broker traffic - hence 429s on PactFlow.
/// Each publish also only reflects the interactions that matched the filter, but the broker
/// records it as a verification of the WHOLE pact.
/// </summary>
public class FilteredPerTestVerificationTests(OrdersApiFixture fixture, ITestOutputHelper output)
    : IClassFixture<OrdersApiFixture>
{
    private void VerifyFiltered(string description)
    {
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
            .WithFilter(description: description)
            .Verify();
    }

    [Fact]
    public void Order1Exists()
    {
        fixture.Orders.Clear();
        fixture.Orders[1] = new Order(1, "Pending");

        VerifyFiltered("a request for order 1");
    }

    [Fact]
    public void Order2Exists()
    {
        fixture.Orders.Clear();
        fixture.Orders[2] = new Order(2, "Fulfilling");

        VerifyFiltered("a request for order 2");
    }

    [Fact]
    public void MissingOrder()
    {
        fixture.Orders.Clear();

        VerifyFiltered("a request for a missing order");
    }
}
