using System;
using System.Collections.Generic;

namespace PactNet
{
    /// <summary>
    /// Context passed to synchronous message verification handlers
    /// </summary>
    /// <typeparam name="TRequest">Request body type</typeparam>
    /// <typeparam name="TResponse">Response body type</typeparam>
    public class SynchronousMessageContext<TRequest, TResponse>
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SynchronousMessageContext{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="request">Deserialised request body</param>
        /// <param name="response">Deserialised response bodies</param>
        /// <param name="requestMetadata">Request metadata</param>
        /// <param name="responseMetadata">Response metadata in response order</param>
        public SynchronousMessageContext(
            TRequest request,
            IReadOnlyList<TResponse> response,
            IReadOnlyDictionary<string, string> requestMetadata,
            IReadOnlyList<IReadOnlyDictionary<string, string>> responseMetadata)
        {
            this.Request = request;
            this.Response = response ?? throw new ArgumentNullException(nameof(response));
            this.RequestMetadata = requestMetadata ?? throw new ArgumentNullException(nameof(requestMetadata));
            this.ResponseMetadata = responseMetadata ?? throw new ArgumentNullException(nameof(responseMetadata));
        }

        /// <summary>
        /// Gets the deserialised request body
        /// </summary>
        public TRequest Request { get; }

        /// <summary>
        /// Gets the deserialised response bodies in configured order
        /// </summary>
        public IReadOnlyList<TResponse> Response { get; }

        /// <summary>
        /// Gets request metadata
        /// </summary>
        public IReadOnlyDictionary<string, string> RequestMetadata { get; }

        /// <summary>
        /// Gets response metadata in configured response order
        /// </summary>
        public IReadOnlyList<IReadOnlyDictionary<string, string>> ResponseMetadata { get; }
    }
}
