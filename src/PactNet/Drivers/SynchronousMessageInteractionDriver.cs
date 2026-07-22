using System;
using PactNet.Interop;

namespace PactNet.Drivers
{
    /// <summary>
    /// Driver for synchronous request/response message interactions
    /// </summary>
    internal class SynchronousMessageInteractionDriver : AbstractPactDriver, ISynchronousMessageInteractionDriver
    {
        private readonly InteractionHandle interaction;

        /// <summary>
        /// Initialises a new instance of the <see cref="SynchronousMessageInteractionDriver"/> class.
        /// </summary>
        /// <param name="pact">Pact handle</param>
        /// <param name="interaction">Interaction handle</param>
        internal SynchronousMessageInteractionDriver(PactHandle pact, InteractionHandle interaction) : base(pact)
        {
            this.interaction = interaction;
        }

        /// <summary>
        /// Add a provider state to the interaction
        /// </summary>
        /// <param name="description">Provider state description</param>
        public void Given(string description)
            => NativeInterop.Given(this.interaction, description).CheckInteropSuccess();

        /// <summary>
        /// Add a provider state with a parameter to the interaction
        /// </summary>
        /// <param name="description">Provider state description</param>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Parameter value</param>
        public void GivenWithParam(string description, string name, string value)
            => NativeInterop.GivenWithParam(this.interaction, description, name, value).CheckInteropSuccess();

        /// <summary>
        /// Set the request contents of the message
        /// </summary>
        /// <param name="contentType">the content type</param>
        /// <param name="body">the body of the message</param>
        /// <param name="size">the size of the message</param>
        public void WithRequestContents(string contentType, string body, uint size)
            => NativeInterop.WithBody(this.interaction, InteractionPart.Request, contentType, body).CheckInteropSuccess();

        /// <summary>
        /// Add a response body to the message
        /// </summary>
        /// <param name="contentType">the content type</param>
        /// <param name="body">the body of the message</param>
        /// <param name="size">the size of the message</param>
        public void WithResponseContents(string contentType, string body, uint size)
            => NativeInterop.WithBody(this.interaction, InteractionPart.Response, contentType, body).CheckInteropSuccess();
    }
}
