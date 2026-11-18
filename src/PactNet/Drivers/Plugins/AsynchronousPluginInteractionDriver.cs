using System.Collections.Generic;
using PactNet.Drivers.Message;
using PactNet.Interop;

namespace PactNet.Drivers.Plugins
{
    internal interface IAsynchronousPluginInteractionDriver : IMessageInteractionDriver
    {
        void WithPluginContent(string contentType, Dictionary<string, object> content);
    }

    internal class AsynchronousPluginInteractionDriver : MessageInteractionDriver, IAsynchronousPluginInteractionDriver
    {
        private readonly PluginInteractionDriver plugin;

        internal AsynchronousPluginInteractionDriver(PactHandle pact, InteractionHandle interaction) : base(pact, interaction)
        {
            this.plugin = new PluginInteractionDriver(interaction);
        }

        public void WithPluginContent(string contentType, Dictionary<string, object> content)
            => this.plugin.WithContent(InteractionPart.Request, contentType, content);
    }
}