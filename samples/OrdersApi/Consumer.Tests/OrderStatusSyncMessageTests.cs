using System.Collections.Generic;
using System.Text.Json;
using PactNet;
using PactNet.Matchers;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Consumer.Tests
{
    public class OrderStatusSyncMessageTests
    {
        private readonly ISynchronousMessagePactBuilderV4 pact;

        public OrderStatusSyncMessageTests(ITestOutputHelper output)
        {
            var config = new PactConfig
            {
                PactDir = "../../../pacts/",
                Outputters = new[]
                {
                    new XunitOutput(output)
                },
                DefaultJsonSettings = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                }
            };

            this.pact = Pact.V4("Fulfilment API", "Orders API", config).WithSynchronousMessageInteractions();
        }

        [Fact]
        public void HandleSyncMessage_OrderStatusUpdateRequest_ReturnsResponse()
        {
            this.pact
                .ExpectsToReceive("a synchronous message requesting that an order status is updated")
                .Given("an order with ID {id} exists", new Dictionary<string, string> { ["id"] = "1" })
                .WithRequestJsonContent(new
                {
                    Id = Match.Integer(1),
                    RequestedStatus = Match.Integer((int)OrderStatus.Fulfilling)
                })
                .WithResponseJsonContent(new
                {
                    Id = 1,
                    Status = OrderStatus.Fulfilling,
                    Accepted = true
                })
                .VerifyWithResponse<JsonElement, OrderStatusUpdateResponse>(request =>
                {
                    int id = ReadMatcherAwareInt(request, "id");
                    int requestedStatus = ReadMatcherAwareInt(request, "requestedStatus");

                    return new OrderStatusUpdateResponse(id, (OrderStatus)requestedStatus, true);
                });
        }

        private static int ReadMatcherAwareInt(JsonElement request, string property)
        {
            JsonElement value = request.GetProperty(property);

            if (value.ValueKind == JsonValueKind.Number)
            {
                return value.GetInt32();
            }

            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("value", out JsonElement wrappedValue))
            {
                return wrappedValue.GetInt32();
            }

            throw new JsonException($"Unable to read integer '{property}' from synchronous message request");
        }

        private record OrderStatusUpdateResponse(int Id, OrderStatus Status, bool Accepted);
    }
}
