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
        /// Add metadata to the most recently configured response body
        /// </summary>
        /// <param name="key">metadata key</param>
        /// <param name="value">metadata value</param>
        /// <returns>Configured message</returns>
        IConfiguredSynchronousMessageVerifierV4 WithResponseMetadata(string key, string value);

        /// <summary>
        /// Add an additional response content body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <returns>Configured message</returns>
        IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body);

        /// <summary>
        /// Add an additional response content body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Configured message</returns>
        IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body, System.Text.Json.JsonSerializerOptions settings);

        /// <summary>
        /// Verify a synchronous message request is read and handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The method using the request message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        void Verify<TRequest>(Action<TRequest> handler);

        /// <summary>
        /// Verify a synchronous message request/response is handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The method using the request and expected response message(s)</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        void Verify<TRequest, TResponse>(Action<SynchronousMessageContext<TRequest, TResponse>> handler);

        /// <summary>
        /// Verify a synchronous message request is read and handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The method using the request message</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        Task VerifyAsync<TRequest>(Func<TRequest, Task> handler);

        /// <summary>
        /// Verify a synchronous message request/response is handled correctly and write the pact
        /// </summary>
        /// <param name="handler">The async method using the request and expected response message(s)</param>
        /// <exception cref="PactMessageConsumerVerificationException">Failed to verify the message</exception>
        Task VerifyAsync<TRequest, TResponse>(Func<SynchronousMessageContext<TRequest, TResponse>, Task> handler);

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
