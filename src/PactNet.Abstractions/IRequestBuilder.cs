using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using PactNet.Matchers;

namespace PactNet
{
    /// <summary>
    /// Build up a mock request for a v2 pact
    /// </summary>
    public interface IRequestBuilderV2
    {
        /// <summary>
        /// Add a provider state
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 Given(string providerState);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithRequest(HttpMethod method, string path);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithRequest(string method, string path);

        /// <summary>
        /// Add a query string parameter
        /// </summary>
        /// <param name="key">Query parameter key</param>
        /// <param name="value">Query parameter value</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>You can add a query parameter with the same key multiple times</remarks>
        IRequestBuilderV2 WithQuery(string key, string value);

        /// <summary>
        /// Add a request header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithHeader(string key, string value);

        /// <summary>
        /// Add a request header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a request header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV2 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a request header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV2 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithJsonBody(dynamic body, string contentType);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithJsonBody(dynamic body, JsonSerializerOptions settings, string contentType);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the request 
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV2 WithBody(string body, string contentType);

        /// <summary>
        /// Define the response to this request
        /// </summary>
        /// <returns>Response builder</returns>
        IResponseBuilderV2 WillRespond();
    }

    /// <summary>
    /// Build up a mock request for a v3 pact
    /// </summary>
    public interface IRequestBuilderV3
    {
        /// <summary>
        /// Add a provider state
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 Given(string providerState);

        /// <summary>
        /// Add a provider state with one or more parameters
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <param name="parameters">Provider state parameters</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 Given(string providerState, IDictionary<string, string> parameters);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithRequest(HttpMethod method, string path);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithRequest(string method, string path);

        /// <summary>
        /// Add a query string parameter
        /// </summary>
        /// <param name="key">Query parameter key</param>
        /// <param name="value">Query parameter value</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>You can add a query parameter with the same key multiple times</remarks>
        IRequestBuilderV3 WithQuery(string key, string value);

        /// <summary>
        /// Add a query parameter matcher
        /// </summary>
        /// <param name="key">Query parameter key</param>
        /// <param name="matcher">Query parameter matcher</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>You can add a query parameter with the same key multiple times</remarks>
        IRequestBuilderV3 WithQuery(string key, IMatcher matcher);

        /// <summary>
        /// Add a request header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithHeader(string key, string value);

        /// <summary>
        /// Add a request header matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a request header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV3 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a request header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV3 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithJsonBody(dynamic body, string contentType);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithJsonBody(dynamic body, JsonSerializerOptions settings, string contentType);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the request 
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV3 WithBody(string body, string contentType);

        // TODO: Support binary and multi-part body

        /// <summary>
        /// Define the response to this request
        /// </summary>
        /// <returns>Response builder</returns>
        IResponseBuilderV3 WillRespond();
    }

    /// <summary>
    /// Build up a mock request for a v4 pact
    /// </summary>
    public interface IRequestBuilderV4
    {
        /// <summary>
        /// Add a provider state
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 Given(string providerState);

        /// <summary>
        /// Add a provider state with one or more parameters
        /// </summary>
        /// <param name="providerState">Provider state description</param>
        /// <param name="parameters">Provider state parameters</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 Given(string providerState, IDictionary<string, string> parameters);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithRequest(HttpMethod method, string path);

        /// <summary>
        /// Set the request
        /// </summary>
        /// <param name="method">Request method</param>
        /// <param name="path">Request path</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithRequest(string method, string path);

        /// <summary>
        /// Add a query string parameter
        /// </summary>
        /// <param name="key">Query parameter key</param>
        /// <param name="value">Query parameter value</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>You can add a query parameter with the same key multiple times</remarks>
        IRequestBuilderV4 WithQuery(string key, string value);

        /// <summary>
        /// Add a query parameter matcher
        /// </summary>
        /// <param name="key">Query parameter key</param>
        /// <param name="matcher">Query parameter matcher</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>You can add a query parameter with the same key multiple times</remarks>
        IRequestBuilderV4 WithQuery(string key, IMatcher matcher);

        /// <summary>
        /// Add a request header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithHeader(string key, string value);

        /// <summary>
        /// Add a request header matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a request header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV4 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a request header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IRequestBuilderV4 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithJsonBody(dynamic body, string contentType);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// Set a body which is serialised as JSON
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <param name="contentType">Content type override</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithJsonBody(dynamic body, JsonSerializerOptions settings, string contentType);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the request 
        /// </summary>
        /// <param name="body">Request body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IRequestBuilderV4 WithBody(string body, string contentType);

        // TODO: Support binary and multi-part body

        /// <summary>
        /// Define the response to this request
        /// </summary>
        /// <returns>Response builder</returns>
        IResponseBuilderV4 WillRespond();
    }
}
