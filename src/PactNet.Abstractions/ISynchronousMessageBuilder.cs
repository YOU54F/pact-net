using System.Collections.Generic;
using System.Text.Json;

namespace PactNet
{
    /// <summary>
    /// Build up a mock synchronous request/response message for a v4 message pact
    /// </summary>
    public interface ISynchronousMessageBuilderV4
    {
        /// <summary>
        /// Add a provider state
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessageBuilderV4 Given(string providerState);

        /// <summary>
        /// Add a provider state with one or more parameters
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <param name="parameters">Provider state parameters</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessageBuilderV4 Given(string providerState, IDictionary<string, string> parameters);

        /// <summary>
        /// Set request content which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessageBuilderV4 WithRequestJsonContent(dynamic body);

        /// <summary>
        /// Set request content which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        ISynchronousMessageBuilderV4 WithRequestJsonContent(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// Set response content which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <returns>Configured message</returns>
        IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body);

        /// <summary>
        /// Set response content which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Configured message</returns>
        IConfiguredSynchronousMessageVerifierV4 WithResponseJsonContent(dynamic body, JsonSerializerOptions settings);
    }
}
