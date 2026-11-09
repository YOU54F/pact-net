using System.Net;
using System.Text.Json;
using PactNet.Matchers;

namespace PactNet
{
    /// <summary>
    /// Mock response builder for a v2 pact
    /// </summary>
    public interface IResponseBuilderV2
    {
        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithStatus(HttpStatusCode status);

        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithStatus(ushort status);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithHeader(string key, string value);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a response header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV2 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a response header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV2 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the response 
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV2 WithBody(string body, string contentType);
    }

    /// <summary>
    /// Mock response builder for a v3 pact
    /// </summary>
    public interface IResponseBuilderV3
    {
        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithStatus(HttpStatusCode status);

        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithStatus(ushort status);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithHeader(string key, string value);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a response header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV3 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a response header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV3 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the response 
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV3 WithBody(string body, string contentType);
    }

    /// <summary>
    /// Mock response builder for a v4 pact
    /// </summary>
    public interface IResponseBuilderV4
    {
        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithStatus(HttpStatusCode status);

        /// <summary>
        /// Set response status code
        /// </summary>
        /// <param name="status">Response status code</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithStatus(ushort status);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithHeader(string key, string value);

        /// <summary>
        /// Add a response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matcher">Header value matcher</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithHeader(string key, IMatcher matcher);

        /// <summary>
        /// Add a response header with multiple values, e.g. a header which appears on multiple field lines
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="values">Header values, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, value)</c> once per value. Each value is added at the next index for
        /// that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV4 WithHeader(string key, params string[] values);

        /// <summary>
        /// Add a response header with multiple values, each with its own matcher
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="matchers">Header value matchers, in order</param>
        /// <returns>Fluent builder</returns>
        /// <remarks>
        /// Equivalent to calling <c>WithHeader(key, matcher)</c> once per matcher. Each matcher is added at the next index
        /// for that header, so values added by previous calls for the same key are kept and these are appended after them.
        /// </remarks>
        IResponseBuilderV4 WithHeader(string key, params IMatcher[] matchers);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithJsonBody(dynamic body);

        /// <summary>
        /// Set a response body which is serialised as JSON
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="settings">Custom JSON serializer settings</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithJsonBody(dynamic body, JsonSerializerOptions settings);

        /// <summary>
        /// A pre-formatted body which should be used as-is for the response 
        /// </summary>
        /// <param name="body">Response body</param>
        /// <param name="contentType">Content type</param>
        /// <returns>Fluent builder</returns>
        IResponseBuilderV4 WithBody(string body, string contentType);
    }
}
