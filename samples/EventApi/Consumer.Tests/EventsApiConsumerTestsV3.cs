using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Consumer.Models;
using FluentAssertions;
using System.Text.Json;
using PactNet;
using PactNet.Matchers;
using Xunit;
using Xunit.Abstractions;

namespace Consumer.Tests
{
    public class EventsApiConsumerTestsV3
    {
        private const string Token = "SomeValidAuthToken";

        private readonly IPactBuilderV3 pact;

        public EventsApiConsumerTestsV3(ITestOutputHelper output)
        {
            var config = new PactConfig
            {
                PactDir = "../../../pacts/",
                Outputters = new[]
                {
                    new XUnitOutput(output)
                },
                DefaultJsonSettings = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                }
            };

            var pact = Pact.V3("Event API ConsumerV3", "Event API", config);
            this.pact = pact.WithHttpInteractions();
        }

        [Fact]

        // Feature not supported on Windows
        public async Task UploadImage_WhenTheFileExists_Returns201()
        {

            string contentType = "image/jpeg";

            var file = new FileInfo("test_file.jpeg");


            this.pact
                .UponReceiving($"a request to upload a file")
                    .WithRequest(HttpMethod.Post, $"/events/upload-file")
                    .WithFileUpload(contentType, file, "file")
                .WillRespond()
                    .WithStatus(201);

            await this.pact.VerifyAsync(async ctx =>
            {
                var client = new EventsApiClient(ctx.MockServerUri, Token);

                var result = await client.UploadFile(file);

                Assert.Equal(201, (int)result);
            });
        }
    }
}
