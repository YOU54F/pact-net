using PactNet.Drivers;
using PactNet.Drivers.Http;
using PactNet.Interop;

namespace PactNet.Compatibility.Tests.Support
{
    /// <summary>
    /// HTTP pact driver that remembers the handle of the most recently created interaction, so that
    /// parts the DSL can't express (binary bodies) can be added through the FFI directly
    /// </summary>
    internal sealed class CapturingHttpPactDriver : IHttpPactDriver
    {
        private readonly PactHandle pact;
        private readonly HttpPactDriver inner;

        public CapturingHttpPactDriver(PactHandle pact)
        {
            this.pact = pact;
            this.inner = new HttpPactDriver(pact);
        }

        public InteractionHandle LastInteraction { get; private set; }

        public IHttpInteractionDriver NewHttpInteraction(string description)
        {
            this.LastInteraction = NativeInterop.NewInteraction(this.pact, description);
            return new HttpInteractionDriver(this.LastInteraction);
        }

        public IMockServerDriver CreateMockServer(string host, int? port, bool tls, string transport = null)
            => this.inner.CreateMockServer(host, port, tls, transport);

        public void WritePactFile(string directory)
            => this.inner.WritePactFile(directory);

        public void WritePactFile(int port, string directory)
            => this.inner.WritePactFile(port, directory);
    }
}
