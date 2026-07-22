using System;
using System.Threading.Tasks;
using PactNet.Exceptions;

namespace PactNet
{
    /// <summary>
    /// Verifies a configured synchronous message interaction
    /// </summary>
    public interface IConfiguredSynchronousMessageVerifierV4
    {
        /// <summary>
        /// Verify a synchronous message request is read and handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The method using the request message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        void Verify<TRequest>(Action<TRequest> handler);

        /// <summary>
        /// Verify a synchronous message request is read and handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The method using the request message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        Task VerifyAsync<TRequest>(Func<TRequest, Task> handler);

        /// <summary>
        /// Verify a synchronous message request is read and handled correctly, and that the response returned by the
        /// handler matches the configured response content, then write the pact
        /// </summary>
        /// <param name="handler">The method using the request message and returning a response message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        void VerifyWithResponse<TRequest, TResponse>(Func<TRequest, TResponse> handler);

        /// <summary>
        /// Verify a synchronous message request is read and handled correctly, and that the response returned by the
        /// async handler matches the configured response content, then write the pact
        /// </summary>
        /// <param name="handler">The async method using the request message and returning a response message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        Task VerifyWithResponseAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler);
    }
}
