using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Provider.Tests
{
    public class EventApiTests : IClassFixture<EventApiFixture>
    {
        private readonly EventApiFixture fixture;
        private readonly PactVerifier verifier;
        private static readonly Uri ProviderUri = new("http://localhost:9222");

        public EventApiTests(EventApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.verifier = new PactVerifier("Event API", new PactVerifierConfig
            {
                LogLevel = PactLogLevel.Debug,
                Outputters = new List<IOutput>
                {
                    new XunitOutput(output)
                }
            });
        }

        [Fact]
        public void EnsureEventApiHonoursPactWithConsumerV3()
        {

            string pactPath = Path.Combine("..",
                                           "..",
                                           "..",
                                           "..",
                                           "Consumer.Tests",
                                           "pacts",
                                           "Event API ConsumerV3-Event API.json");

            this.verifier
                .WithHttpEndpoint(ProviderUri)
                .WithFileSource(new FileInfo(pactPath))
                .WithRequestTimeout(TimeSpan.FromSeconds(2))
                .WithSslVerificationDisabled()
                .Verify();
        }
    }
}
