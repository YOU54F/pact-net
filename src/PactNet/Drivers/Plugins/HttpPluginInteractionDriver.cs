using System.Collections.Generic;
using PactNet.Drivers.Http;
using PactNet.Interop;

namespace PactNet.Drivers.Plugins
{
    internal interface IHttpPluginInteractionDriver : IHttpInteractionDriver
    {
        void WithPluginContent(InteractionPart part, string contentType, Dictionary<string, object> content);
    }

    internal class HttpPluginInteractionDriver : IHttpPluginInteractionDriver
    {
        private readonly HttpInteractionDriver http;
        private readonly PluginInteractionDriver plugin;

        internal HttpPluginInteractionDriver(InteractionHandle interaction)
        {
            this.http = new HttpInteractionDriver(interaction);
            this.plugin = new PluginInteractionDriver(interaction);
        }

        public void Given(string description) => this.http.Given(description);
        public void GivenWithParam(string description, string name, string value) => this.http.GivenWithParam(description, name, value);
        public void WithRequest(string method, string path) => this.http.WithRequest(method, path);
        public void WithQueryParameter(string name, string value, uint index) => this.http.WithQueryParameter(name, value, index);
        public void WithRequestHeader(string name, string value, uint index) => this.http.WithRequestHeader(name, value, index);
        public void WithResponseHeader(string name, string value, uint index) => this.http.WithResponseHeader(name, value, index);
        public void WithResponseStatus(ushort status) => this.http.WithResponseStatus(status);
        public void WithRequestBody(string contentType, string body) => this.http.WithRequestBody(contentType, body);
        public void WithResponseBody(string contentType, string body) => this.http.WithResponseBody(contentType, body);
        public void WithPluginContent(InteractionPart part, string contentType, Dictionary<string, object> content)
            => this.plugin.WithContent(part, contentType, content);
    }
}