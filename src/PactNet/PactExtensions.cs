using PactNet.Drivers;
using PactNet.Drivers.Http;
using PactNet.Drivers.Message;
using PactNet.Drivers.Plugins;
using PactNet.Interop;
using PactNet.Models;

namespace PactNet
{
    /// <summary>
    /// Extensions for <see cref="Pact"/>
    /// </summary>
    public static class PactExtensions
    {
        /// <summary>
        /// Establish a new pact using the native backend
        /// </summary>
        /// <param name="pact">Pact details</param>
        /// <param name="port">Port for the mock server. If null, one will be assigned automatically</param>
        /// <param name="host">Host for the mock server</param>
        /// <returns>Pact builder</returns>
        /// <remarks>
        /// If multiple mock servers are started at the same time, you must make sure you don't supply the same port twice.
        /// It is advised that the port is not specified whenever possible to allow PactNet to allocate a port dynamically
        /// and ensure there are no port clashes
        /// </remarks>
        public static IPactBuilderV2 WithHttpInteractions(this IPactV2 pact, int? port = null, IPAddress host = IPAddress.Loopback)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IHttpPactDriver httpPact = driver.NewHttpPact(pact.Consumer, pact.Provider, PactSpecification.V2);

            var builder = new PactBuilder(httpPact, pact.Config, port, host);
            return builder;
        }

        /// <summary>
        /// Establish a new pact using the native backend
        /// </summary>
        /// <param name="pact">Pact details</param>
        /// <param name="port">Port for the mock server. If null, one will be assigned automatically</param>
        /// <param name="host">Host for the mock server</param>
        /// <returns>Pact builder</returns>
        /// <remarks>
        /// If multiple mock servers are started at the same time, you must make sure you don't supply the same port twice.
        /// It is advised that the port is not specified whenever possible to allow PactNet to allocate a port dynamically
        /// and ensure there are no port clashes
        /// </remarks>
        public static IPactBuilderV3 WithHttpInteractions(this IPactV3 pact, int? port = null, IPAddress host = IPAddress.Loopback)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IHttpPactDriver httpPact = driver.NewHttpPact(pact.Consumer, pact.Provider, PactSpecification.V3);

            var builder = new PactBuilder(httpPact, pact.Config, port, host);
            return builder;
        }

        /// <summary>
        /// Establish a new pact using the native backend
        /// </summary>
        /// <param name="pact">Pact details</param>
        /// <param name="port">Port for the mock server. If null, one will be assigned automatically</param>
        /// <param name="host">Host for the mock server</param>
        /// <returns>Pact builder</returns>
        /// <remarks>
        /// If multiple mock servers are started at the same time, you must make sure you don't supply the same port twice.
        /// It is advised that the port is not specified whenever possible to allow PactNet to allocate a port dynamically
        /// and ensure there are no port clashes
        /// </remarks>
        public static IPactBuilderV4 WithHttpInteractions(this IPactV4 pact, int? port = null, IPAddress host = IPAddress.Loopback)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IHttpPactDriver httpPact = driver.NewHttpPact(pact.Consumer, pact.Provider, PactSpecification.V4);

            var builder = new PactBuilder(httpPact, pact.Config, port, host);
            return builder;
        }

        /// <summary>
        /// Add asynchronous message (i.e. consumer/producer) interactions to the pact
        /// </summary>
        /// <param name="pact">Pact details</param>
        /// <returns>Pact builder</returns>
        public static IMessagePactBuilderV3 WithMessageInteractions(this IPactV3 pact)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IMessagePactDriver messagePact = driver.NewMessagePact(pact.Consumer, pact.Provider, PactSpecification.V3);

            var builder = new MessagePactBuilder(messagePact, pact.Config, PactSpecification.V3);
            return builder;
        }

        /// <summary>
        /// Add asynchronous message (i.e. consumer/producer) interactions to the pact
        /// </summary>
        /// <param name="pact">Pact details</param>
        /// <returns>Pact builder</returns>
        public static IMessagePactBuilderV4 WithMessageInteractions(this IPactV4 pact)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IMessagePactDriver messagePact = driver.NewMessagePact(pact.Consumer, pact.Provider, PactSpecification.V4);

            var builder = new MessagePactBuilder(messagePact, pact.Config, PactSpecification.V4);
            return builder;
        }

        /// <summary>
        /// Establish a new pact with synchronous plugin interactions.
        /// </summary>
        /// <param name="pact"></param>
        /// <param name="pluginName">Plugin name</param>
        /// <param name="pluginVersion">Plugin version</param>
        /// <param name="transport">The transport to use (i.e. http, https, grpc). Must be a valid UTF-8 NULL-terminated string, or NULL or empty, in which case http will be used.</param>
        /// <param name="port">Port for the mock server. If null, one will be assigned automatically</param>
        /// <param name="host">Host for the mock server</param>
        /// <returns>Synchronous plugin builder</returns>
        public static ISynchronousPluginPactBuilderV4 WithSynchronousPluginInteractions(this IPactV4 pact,
            string pluginName,
            string pluginVersion,
            string transport = null,
            int? port = null,
            IPAddress host = IPAddress.Loopback)
        {
            pact.Config.LogLevel.LogToBuffer();

            IPactDriver driver = new PactDriver();
            IPluginPactDriver pluginDriver = driver.NewPluginPact(pact.Consumer, pact.Provider, pluginName, pluginVersion, PactSpecification.V4);

            return new SynchronousPluginPactBuilder(pluginDriver, pact.Config, port, host, transport);
        }

        /// <summary>Create HTTP interactions with plugin-defined request or response contents.</summary>
        /// <param name="pact">Pact details.</param>
        /// <param name="pluginName">Content plugin name.</param>
        /// <param name="pluginVersion">Optional plugin version.</param>
        /// <param name="port">Optional mock server port.</param>
        /// <param name="host">Mock server host.</param>
        /// <returns>HTTP plugin builder.</returns>
        public static IHttpPluginPactBuilderV4 WithHttpPluginInteractions(this IPactV4 pact,
            string pluginName, string pluginVersion = null, int? port = null,
            IPAddress host = IPAddress.Loopback)
        {
            pact.Config.LogLevel.LogToBuffer();
            IPactDriver driver = new PactDriver();
            IPluginPactDriver pluginDriver = driver.NewPluginPact(pact.Consumer, pact.Provider,
                pluginName, pluginVersion, PactSpecification.V4);
            return new HttpPluginPactBuilder(pluginDriver, pact.Config, port, host);
        }

        /// <summary>Create asynchronous plugin messages without a mock-server transport.</summary>
        /// <param name="pact">Pact details.</param>
        /// <param name="pluginName">Content plugin name.</param>
        /// <param name="pluginVersion">Optional plugin version.</param>
        /// <returns>Asynchronous plugin message builder.</returns>
        public static IAsynchronousPluginPactBuilderV4 WithAsynchronousPluginInteractions(this IPactV4 pact,
            string pluginName, string pluginVersion = null)
        {
            pact.Config.LogLevel.LogToBuffer();
            IPactDriver driver = new PactDriver();
            IPluginPactDriver pluginDriver = driver.NewPluginPact(pact.Consumer, pact.Provider,
                pluginName, pluginVersion, PactSpecification.V4);
            return new AsynchronousPluginPactBuilder(pluginDriver, pact.Config);
        }
    }
}
