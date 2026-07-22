using System;
using System.Text.Json;
using System.Threading.Tasks;
using PactNet.Drivers;
using PactNet.Exceptions;

namespace PactNet
{
    /// <summary>
    /// Verifies a configured synchronous message interaction
    /// </summary>
    internal class ConfiguredSynchronousMessageVerifier : IConfiguredSynchronousMessageVerifierV4
    {
        private readonly ISynchronousMessageInteractionDriver driver;
        private readonly PactConfig config;
        private readonly string requestBody;
        private readonly string responseBody;

        /// <summary>
        /// Initialises a new instance of the <see cref="ConfiguredSynchronousMessageVerifier"/> class.
        /// </summary>
        /// <param name="driver">Pact driver</param>
        /// <param name="config">Pact configuration</param>
        /// <param name="requestBody">Serialized request body</param>
        /// <param name="responseBody">Serialized response body</param>
        internal ConfiguredSynchronousMessageVerifier(ISynchronousMessageInteractionDriver driver, PactConfig config, string requestBody, string responseBody)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.requestBody = requestBody;
            this.responseBody = responseBody;
        }

        /// <inheritdoc />
        public void Verify<TRequest>(Action<TRequest> handler)
        {
            try
            {
                TRequest request = DeserializeRequest<TRequest>();
                handler(request);
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (PactMessageConsumerVerificationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PactMessageConsumerVerificationException("The synchronous message could not be verified by the consumer handler", e);
            }
        }

        /// <inheritdoc />
        public async Task VerifyAsync<TRequest>(Func<TRequest, Task> handler)
        {
            try
            {
                TRequest request = DeserializeRequest<TRequest>();
                await handler(request);
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (PactMessageConsumerVerificationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PactMessageConsumerVerificationException("The synchronous message could not be verified by the consumer handler", e);
            }
        }

        /// <inheritdoc />
        public void VerifyWithResponse<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            try
            {
                TRequest request = DeserializeRequest<TRequest>();
                TResponse actualResponse = handler(request);
                VerifyResponse(actualResponse);
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (PactMessageConsumerVerificationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PactMessageConsumerVerificationException("The synchronous message could not be verified by the consumer handler", e);
            }
        }

        /// <inheritdoc />
        public async Task VerifyWithResponseAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        {
            try
            {
                TRequest request = DeserializeRequest<TRequest>();
                TResponse actualResponse = await handler(request);
                VerifyResponse(actualResponse);
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (PactMessageConsumerVerificationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PactMessageConsumerVerificationException("The synchronous message could not be verified by the consumer handler", e);
            }
        }

        private TRequest DeserializeRequest<TRequest>()
        {
            if (string.IsNullOrWhiteSpace(this.requestBody))
            {
                throw new InvalidOperationException("Request content must be configured before verifying a synchronous message interaction");
            }

            return JsonSerializer.Deserialize<TRequest>(this.requestBody, this.config.DefaultJsonSettings);
        }

        private void VerifyResponse<TResponse>(TResponse actualResponse)
        {
            if (string.IsNullOrWhiteSpace(this.responseBody))
            {
                throw new InvalidOperationException("Response content must be configured before response verification can run");
            }

            string actualResponseJson = JsonSerializer.Serialize(actualResponse, this.config.DefaultJsonSettings);

            using JsonDocument expectedDocument = JsonDocument.Parse(this.responseBody);
            using JsonDocument actualDocument = JsonDocument.Parse(actualResponseJson);

            if (!JsonElementsEqual(expectedDocument.RootElement, actualDocument.RootElement))
            {
                throw new PactMessageConsumerVerificationException("The synchronous message response did not match the configured response payload");
            }
        }

        private static bool JsonElementsEqual(JsonElement expected, JsonElement actual)
        {
            if (expected.ValueKind != actual.ValueKind)
            {
                return false;
            }

            switch (expected.ValueKind)
            {
                case JsonValueKind.Object:
                    var expectedProperties = new System.Collections.Generic.Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        expectedProperties[property.Name] = property.Value;
                    }

                    var actualProperties = new System.Collections.Generic.Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        actualProperties[property.Name] = property.Value;
                    }

                    if (expectedProperties.Count != actualProperties.Count)
                    {
                        return false;
                    }

                    foreach (var property in expectedProperties)
                    {
                        if (!actualProperties.TryGetValue(property.Key, out JsonElement actualValue))
                        {
                            return false;
                        }

                        if (!JsonElementsEqual(property.Value, actualValue))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedArray = expected.EnumerateArray();
                    JsonElement.ArrayEnumerator actualArray = actual.EnumerateArray();

                    var expectedItems = new System.Collections.Generic.List<JsonElement>();
                    var actualItems = new System.Collections.Generic.List<JsonElement>();

                    while (expectedArray.MoveNext())
                    {
                        expectedItems.Add(expectedArray.Current);
                    }

                    while (actualArray.MoveNext())
                    {
                        actualItems.Add(actualArray.Current);
                    }

                    if (expectedItems.Count != actualItems.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < expectedItems.Count; i++)
                    {
                        if (!JsonElementsEqual(expectedItems[i], actualItems[i]))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.String:
                    return expected.GetString() == actual.GetString();

                case JsonValueKind.Number:
                    return expected.GetRawText() == actual.GetRawText();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return expected.GetBoolean() == actual.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return true;

                default:
                    return expected.GetRawText() == actual.GetRawText();
            }
        }
    }
}
