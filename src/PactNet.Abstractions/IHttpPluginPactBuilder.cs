using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PactNet
{
    /// <summary>HTTP interactions using a plugin to configure and match their contents.</summary>
    public interface IHttpPluginPactBuilderV4 : IPactBuilder, IDisposable
    {
        /// <summary>Start an HTTP interaction.</summary>
        IHttpPluginRequestBuilderV4 UponReceiving(string description);
    }

    /// <summary>Configure an HTTP request before defining its response.</summary>
    public interface IHttpPluginRequestBuilderV4
    {
        /// <summary>Add a provider state.</summary>
        IHttpPluginRequestBuilderV4 Given(string description);
        /// <summary>Add a parameterized provider state.</summary>
        IHttpPluginRequestBuilderV4 Given(string description, string name, string value);
        /// <summary>Set the HTTP method and path.</summary>
        IHttpPluginRequestBuilderV4 WithRequest(HttpMethod method, string path);
        /// <summary>Add a query parameter.</summary>
        IHttpPluginRequestBuilderV4 WithQuery(string name, string value);
        /// <summary>Add a request header.</summary>
        IHttpPluginRequestBuilderV4 WithHeader(string name, string value);
        /// <summary>Configure request contents once using the plugin's contents schema.</summary>
        IHttpPluginRequestBuilderV4 WithContent(string contentType, Dictionary<string, object> content);
        /// <summary>Complete the request and begin the response.</summary>
        IHttpPluginResponseBuilderV4 WillRespondWithStatus(int status);
    }

    /// <summary>Configure an HTTP response using plugin contents.</summary>
    public interface IHttpPluginResponseBuilderV4
    {
        /// <summary>Add a response header before setting contents.</summary>
        IHttpPluginResponseBuilderV4 WithHeader(string name, string value);
        /// <summary>Configure response contents once, completing this interaction.</summary>
        void WithContent(string contentType, Dictionary<string, object> content);
    }
}