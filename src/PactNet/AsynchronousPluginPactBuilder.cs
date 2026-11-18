using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PactNet.Drivers.Plugins;
using PactNet.Exceptions;

namespace PactNet
{
    internal class AsynchronousPluginPactBuilder : IAsynchronousPluginPactBuilderV4
    {
        private readonly IPluginPactDriver driver;
        private readonly PactConfig config;

        internal AsynchronousPluginPactBuilder(IPluginPactDriver driver, PactConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        public IAsynchronousPluginMessageBuilderV4 ExpectsToReceive(string description)
            => new AsynchronousPluginMessageBuilder(this.driver.NewAsynchronousPluginInteraction(description), this.config);

        public void Dispose() => this.driver.Dispose();
    }

    internal class AsynchronousPluginMessageBuilder : IAsynchronousPluginMessageBuilderV4
    {
        private readonly IAsynchronousPluginInteractionDriver driver;
        private readonly PactConfig config;
        private readonly Dictionary<string, string> metadata = new();
        private bool completed;

        internal AsynchronousPluginMessageBuilder(IAsynchronousPluginInteractionDriver driver, PactConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        private void EnsureMutable()
        {
            if (this.completed) throw new InvalidOperationException("Message contents have already been configured.");
        }

        public IAsynchronousPluginMessageBuilderV4 Given(string description)
        {
            this.EnsureMutable();
            this.driver.Given(description);
            return this;
        }

        public IAsynchronousPluginMessageBuilderV4 Given(string description, string name, string value)
        {
            this.EnsureMutable();
            this.driver.GivenWithParam(description, name, value);
            return this;
        }

        public IAsynchronousPluginMessageBuilderV4 WithMetadata(string name, string value)
        {
            this.EnsureMutable();
            this.metadata[name] = value;
            return this;
        }

        public IConfiguredPluginMessageVerifier WithContent(string contentType, Dictionary<string, object> content)
        {
            this.EnsureMutable();
            this.driver.WithPluginContent(contentType, content);
            foreach (var property in this.metadata)
                this.driver.WithMetadata(property.Key, property.Value);
            this.completed = true;
            return new ConfiguredPluginMessageVerifier(this.driver, this.config);
        }
    }

    internal class ConfiguredPluginMessageVerifier : IConfiguredPluginMessageVerifier
    {
        private readonly IAsynchronousPluginInteractionDriver driver;
        private readonly PactConfig config;

        internal ConfiguredPluginMessageVerifier(IAsynchronousPluginInteractionDriver driver, PactConfig config)
        {
            this.driver = driver;
            this.config = config;
        }

        internal static PluginMessage Decode(string reified)
        {
            using var document = JsonDocument.Parse(reified);
            var contents = document.RootElement.GetProperty("contents");
            string content = contents.GetProperty("content").GetString();
            bool encoded = contents.TryGetProperty("encoded", out var encoding)
                && (encoding.ValueKind == JsonValueKind.True
                    || (encoding.ValueKind == JsonValueKind.String && encoding.GetString() == "base64"));
            byte[] bytes = encoded ? Convert.FromBase64String(content) : Encoding.UTF8.GetBytes(content);
            var metadata = new Dictionary<string, object>();
            if (document.RootElement.TryGetProperty("metadata", out var properties))
            {
                foreach (var property in properties.EnumerateObject())
                    metadata[property.Name] = property.Value.Clone();
            }
            return new PluginMessage(bytes, contents.GetProperty("contentType").GetString(), metadata);
        }

        public void Verify(Action<PluginMessage> consumer)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            try
            {
                consumer(Decode(this.driver.Reify()));
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (Exception exception)
            {
                throw new PactMessageConsumerVerificationException("The plugin message could not be verified by the consumer handler.", exception);
            }
        }

        public async Task VerifyAsync(Func<PluginMessage, Task> consumer)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            try
            {
                await consumer(Decode(this.driver.Reify()));
                this.driver.WritePactFile(this.config.PactDir);
            }
            catch (Exception exception)
            {
                throw new PactMessageConsumerVerificationException("The plugin message could not be verified by the consumer handler.", exception);
            }
        }
    }
}