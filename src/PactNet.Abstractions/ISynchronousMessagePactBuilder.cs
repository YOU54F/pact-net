namespace PactNet
{
    /// <summary>
    /// Synchronous message pact v4 Builder
    /// </summary>
    public interface ISynchronousMessagePactBuilderV4
    {
        /// <summary>
        /// Add a new synchronous request/response message to the pact
        /// </summary>
        /// <param name="description">Message description</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessageBuilderV4 ExpectsToReceive(string description);

        /// <summary>
        /// Add metadata information to message pact
        /// </summary>
        /// <param name="namespace">the parent configuration section</param>
        /// <param name="name">the metadata field value</param>
        /// <param name="value">the metadata field value</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessagePactBuilderV4 WithPactMetadata(string @namespace, string name, string value);
    }
}
