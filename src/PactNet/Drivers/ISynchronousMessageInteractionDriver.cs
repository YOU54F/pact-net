namespace PactNet.Drivers
{
    /// <summary>
    /// Driver for synchronous request/response message interactions
    /// </summary>
    internal interface ISynchronousMessageInteractionDriver : IProviderStateDriver, ICompletedPactDriver
    {
        /// <summary>
        /// Set the request contents of the message
        /// </summary>
        /// <param name="contentType">the content type</param>
        /// <param name="body">the body of the message</param>
        /// <param name="size">the size of the message</param>
        void WithRequestContents(string contentType, string body, uint size);

        /// <summary>
        /// Set metadata for the message interaction
        /// </summary>
        /// <param name="key">metadata key</param>
        /// <param name="value">metadata value</param>
        void WithMetadata(string key, string value);

        /// <summary>
        /// Add a response body to the message
        /// </summary>
        /// <param name="contentType">the content type</param>
        /// <param name="body">the body of the message</param>
        /// <param name="size">the size of the message</param>
        void WithResponseContents(string contentType, string body, uint size);
    }
}
