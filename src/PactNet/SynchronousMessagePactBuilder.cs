using System;
using PactNet.Drivers;
using PactNet.Drivers.Message;

namespace PactNet
{
    /// <summary>
    /// Synchronous message pact builder for Pact v4
    /// </summary>
    internal class SynchronousMessagePactBuilder : ISynchronousMessagePactBuilderV4
    {
        private readonly IMessagePactDriver driver;
        private readonly PactConfig config;

        /// <summary>
        /// Initialises a new instance of the <see cref="SynchronousMessagePactBuilder"/> class.
        /// </summary>
        /// <param name="pact">Pact driver</param>
        /// <param name="config">the message pact configuration</param>
        internal SynchronousMessagePactBuilder(IMessagePactDriver pact, PactConfig config)
        {
            this.driver = pact ?? throw new ArgumentNullException(nameof(pact));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Add a new synchronous request/response message to the message pact
        /// </summary>
        /// <param name="description">Message description</param>
        /// <returns>Fluent builder</returns>
        public ISynchronousMessageBuilderV4 ExpectsToReceive(string description)
        {
            ISynchronousMessageInteractionDriver messageDriver = this.driver.NewSynchronousMessageInteraction(description);
            return new SynchronousMessageBuilder(messageDriver, this.config);
        }

        /// <summary>
        /// Add metadata to the message pact
        /// </summary>
        /// <param name="namespace">the parent configuration section</param>
        /// <param name="name">the metadata field name</param>
        /// <param name="value">the metadata field value</param>
        /// <returns>Fluent builder</returns>
        public ISynchronousMessagePactBuilderV4 WithPactMetadata(string @namespace, string name, string value)
        {
            this.driver.WithMessagePactMetadata(@namespace, name, value);
            return this;
        }
    }
}
