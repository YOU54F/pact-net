using System;
using System.Collections.Generic;
using System.Text.Json;
using PactNet.Drivers;

namespace PactNet
{
    /// <summary>
    /// Synchronous request/response message builder for Pact v4
    /// </summary>
    internal class SynchronousMessageBuilder : ISynchronousMessageBuilderV4
    {
        private readonly ISynchronousMessageInteractionDriver driver;
        private readonly PactConfig config;

        private string requestBody;
        private bool hasResponseContents;
        private readonly Dictionary<string, string> interactionMetadata = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> pendingResponseMetadata = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> responseBodies = new List<string>();
        private readonly List<Dictionary<string, string>> responseMetadata = new List<Dictionary<string, string>>();

        /// <summary>
        /// Initialises a new instance of the <see cref="SynchronousMessageBuilder"/> class.
        /// </summary>
        /// <param name="driver">Interaction driver</param>
        /// <param name="config">Pact config</param>
        internal SynchronousMessageBuilder(ISynchronousMessageInteractionDriver driver, PactConfig config)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 Given(string providerState)
        {
            this.driver.Given(providerState);
            return this;
        }

        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 Given(string providerState, IDictionary<string, string> parameters)
        {
            foreach (var param in parameters)
            {
                this.driver.GivenWithParam(providerState, param.Key, param.Value);
            }

            return this;
        }

        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 WithRequestMetadata(string key, string value)
        {
            this.interactionMetadata[key] = value;
            this.driver.WithRequestMetadata(key, value);

            return this;
        }
        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 WithResponseMetadata(string key, string value)
        {
            this.interactionMetadata[key] = value;
            this.pendingResponseMetadata[key] = value;

            if (this.hasResponseContents)
            {
                this.driver.WithResponseMetadata(key, value);
            }

            return this;
        }

        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 WithRequestJsonContent(dynamic body)
            => this.WithRequestJsonContent(body, this.config.DefaultJsonSettings);

        /// <inheritdoc />
        public ISynchronousMessageBuilderV4 WithRequestJsonContent(dynamic body, JsonSerializerOptions settings)
        {
            string serialised = JsonSerializer.Serialize(body, settings);

            this.requestBody = serialised;
            this.driver.WithRequestContents("application/json", serialised, 0);

            return this;
        }

        /// <inheritdoc />
        public IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body)
            => this.WithResponseJsonContent(body, this.config.DefaultJsonSettings);

        /// <inheritdoc />
        public IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body, JsonSerializerOptions settings)
        {
            string serialised = JsonSerializer.Serialize(body, settings);

            this.responseBodies.Add(serialised);
            this.responseMetadata.Add(new Dictionary<string, string>(this.interactionMetadata, StringComparer.Ordinal));
            this.driver.WithResponseContents("application/json", serialised, 0);
            this.hasResponseContents = true;

            foreach (KeyValuePair<string, string> pair in this.pendingResponseMetadata)
            {
                this.driver.WithResponseMetadata(pair.Key, pair.Value);
            }

            return new ConfiguredSynchronousMessageVerifier(this.driver, this.config, this.requestBody, this.interactionMetadata, this.responseBodies, this.responseMetadata);
        }
    }
}
