using System;
using System.Collections.Generic;
using System.Net.Http;
using PactNet.Drivers.Plugins;
using PactNet.Interop;
using PactNet.Models;

namespace PactNet
{
    internal class HttpPluginPactBuilder : AbstractPactBuilder, IHttpPluginPactBuilderV4
    {
        private readonly IPluginPactDriver pact;

        internal HttpPluginPactBuilder(IPluginPactDriver pact, PactConfig config, int? port = null,
            IPAddress host = IPAddress.Loopback) : base(pact, config, port, host, "http")
        {
            this.pact = pact;
        }

        public IHttpPluginRequestBuilderV4 UponReceiving(string description)
            => new HttpPluginRequestBuilder(this.pact.NewHttpPluginInteraction(description));

        public void Dispose() => this.pact.Dispose();
    }

    internal class HttpPluginRequestBuilder : IHttpPluginRequestBuilderV4
    {
        private readonly IHttpPluginInteractionDriver driver;
        private readonly Dictionary<string, uint> headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, uint> queries = new();
        private bool requestConfigured;
        private bool contentsConfigured;
        private bool responseStarted;

        internal HttpPluginRequestBuilder(IHttpPluginInteractionDriver driver) => this.driver = driver;

        private void EnsureMutable()
        {
            if (this.responseStarted) throw new InvalidOperationException("The request has already been completed.");
        }

        public IHttpPluginRequestBuilderV4 Given(string description)
        {
            this.EnsureMutable();
            this.driver.Given(description);
            return this;
        }

        public IHttpPluginRequestBuilderV4 Given(string description, string name, string value)
        {
            this.EnsureMutable();
            this.driver.GivenWithParam(description, name, value);
            return this;
        }

        public IHttpPluginRequestBuilderV4 WithRequest(HttpMethod method, string path)
        {
            this.EnsureMutable();
            if (this.requestConfigured) throw new InvalidOperationException("Request has already been configured.");
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A request path is required.", nameof(path));
            this.driver.WithRequest(method.Method, path);
            this.requestConfigured = true;
            return this;
        }

        public IHttpPluginRequestBuilderV4 WithQuery(string name, string value)
        {
            this.EnsureMutable();
            this.queries.TryGetValue(name, out uint index);
            this.driver.WithQueryParameter(name, value, index);
            this.queries[name] = index + 1;
            return this;
        }

        public IHttpPluginRequestBuilderV4 WithHeader(string name, string value)
        {
            this.EnsureMutable();
            this.headers.TryGetValue(name, out uint index);
            this.driver.WithRequestHeader(name, value, index);
            this.headers[name] = index + 1;
            return this;
        }

        public IHttpPluginRequestBuilderV4 WithContent(string contentType, Dictionary<string, object> content)
        {
            this.EnsureMutable();
            if (this.contentsConfigured) throw new InvalidOperationException("Request contents have already been configured.");
            this.driver.WithPluginContent(InteractionPart.Request, contentType, content);
            this.contentsConfigured = true;
            return this;
        }

        public IHttpPluginResponseBuilderV4 WillRespondWithStatus(int status)
        {
            this.EnsureMutable();
            if (!this.requestConfigured) throw new InvalidOperationException("Configure the HTTP request first.");
            if (status < 100 || status > 599) throw new ArgumentOutOfRangeException(nameof(status));
            this.driver.WithResponseStatus((ushort)status);
            this.responseStarted = true;
            return new HttpPluginResponseBuilder(this.driver);
        }
    }

    internal class HttpPluginResponseBuilder : IHttpPluginResponseBuilderV4
    {
        private readonly IHttpPluginInteractionDriver driver;
        private readonly Dictionary<string, uint> headers = new(StringComparer.OrdinalIgnoreCase);
        private bool completed;

        internal HttpPluginResponseBuilder(IHttpPluginInteractionDriver driver) => this.driver = driver;

        private void EnsureMutable()
        {
            if (this.completed) throw new InvalidOperationException("Response contents have already been configured.");
        }

        public IHttpPluginResponseBuilderV4 WithHeader(string name, string value)
        {
            this.EnsureMutable();
            this.headers.TryGetValue(name, out uint index);
            this.driver.WithResponseHeader(name, value, index);
            this.headers[name] = index + 1;
            return this;
        }

        public void WithContent(string contentType, Dictionary<string, object> content)
        {
            this.EnsureMutable();
            this.driver.WithPluginContent(InteractionPart.Response, contentType, content);
            this.completed = true;
        }
    }
}