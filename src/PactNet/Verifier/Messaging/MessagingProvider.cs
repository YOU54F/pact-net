using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using PactNet.Exceptions;
using PactNet.Internal;

namespace PactNet.Verifier.Messaging
{
    /// <summary>
    /// Messaging provider service, which simulates messaging responses in order to verify interactions
    /// </summary>
    internal class MessagingProvider : IMessagingProvider
    {
        private const int MinimumPort = 30000;
        private const int MaximumPort = 65535;
        private const int MaxStartAttempts = 25;

        private static readonly JsonSerializerOptions InteractionSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly PactVerifierConfig config;
        private readonly Thread thread;

        private HttpListener server;
        private JsonSerializerOptions defaultSettings;

        /// <summary>
        /// Scenarios configured for the provider
        /// </summary>
        public IMessageScenarios Scenarios { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="MessagingProvider"/> class.
        /// </summary>
        /// <param name="config">Verifier config</param>
        /// <param name="scenarios">Message scenarios</param>
        public MessagingProvider(PactVerifierConfig config, IMessageScenarios scenarios)
        {
            this.config = config;
            this.Scenarios = scenarios;
            this.thread = new Thread(this.HandleRequest);
        }

        /// <summary>
        /// Start the provider service
        /// </summary>
        /// <param name="settings">Default JSON serializer settings</param>
        /// <returns>URI of the started service</returns>
        public Uri Start(JsonSerializerOptions settings)
        {
            Guard.NotNull(settings, nameof(settings));
            this.defaultSettings = settings;

            for (int attempt = 1; attempt <= MaxStartAttempts; attempt++)
            {
                Uri uri;

                try
                {
                    int port = FindUnusedPort();
                    uri = new Uri($"http://localhost:{port}/pact-messages/");

                    this.config.WriteLine($"Starting messaging provider at {uri}");

                    this.server = new HttpListener();
                    this.server.Prefixes.Add(uri.AbsoluteUri);
                    this.server.Start();
                }
                catch (HttpListenerException e) when (IsAddressInUse(e))
                {
                    // There is still a small race between selecting a port and binding the listener.
                    // Retry with a fresh ephemeral port when this happens.
                    this.config.WriteLine($"Failed to start messaging provider because the port is already in use (attempt {attempt}/{MaxStartAttempts}), retrying...");
                    continue;
                }
                catch (Exception e)
                {
                    throw new PactFailureException("Unable to start the internal messaging server", e);
                }

                this.thread.Start();
                return uri;
            }

            throw new PactFailureException($"Unable to start the internal messaging server after {MaxStartAttempts} attempts because all candidate ports were in use");
        }

        /// <summary>
        /// Find an unused port on which to host the messaging server
        /// </summary>
        /// <returns>Unused port</returns>
        /// <exception cref="InvalidOperationException">No local ports were available</exception>
        private static int FindUnusedPort()
        {
            // Ask the OS for an available ephemeral port rather than relying on a stale endpoint snapshot.
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                if (listener.LocalEndpoint is IPEndPoint endpoint)
                {
                    int port = endpoint.Port;

                    if (port is >= MinimumPort and <= MaximumPort)
                    {
                        return port;
                    }

                    throw new InvalidOperationException($"The selected local port {port} is outside the expected ephemeral range");
                }

                throw new InvalidOperationException("Unable to determine the selected local port from the loopback listener");
            }
            finally
            {
                listener.Stop();
            }
        }

        private static bool IsAddressInUse(HttpListenerException exception)
        {
            // EADDRINUSE values by platform:
            // - macOS/BSD: 48
            // - Linux: 98
            // - Windows (WSAEADDRINUSE): 10048
            return exception.ErrorCode is 48 or 98 or 10048
                   || string.Equals(exception.Message, "Address already in use", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Handle an incoming request from the Pact Core messaging driver
        /// </summary>
        private void HandleRequest()
        {
            this.config.WriteLine("Messaging provider successfully started");

            while (this.server.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = this.server.GetContext();
                }
                catch (HttpListenerException)
                {
                    // this thread blocks waiting for the next request, and if the server stops then this exception is raised
                    break;
                }

                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    this.NotAllowedResponse(context, $"WARNING: received messaging request with incorrect method: {context.Request.HttpMethod}");
                    continue;
                }

                MessageInteraction interaction;

                try
                {
                    var reader = new StreamReader(context.Request.InputStream);
                    string body = reader.ReadToEnd();
                    interaction = JsonSerializer.Deserialize<MessageInteraction>(body, InteractionSettings);

                    if (string.IsNullOrWhiteSpace(interaction.Description))
                    {
                        this.BadRequestResponse(context, $"ERROR: The interaction had no description. Request body: {body}");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    this.ErrorResponse(context, $"ERROR: Unable to read message interaction body: {e}");
                    continue;
                }

                this.HandleInteraction(context, interaction);
            }
        }

        /// <summary>
        /// Handle an interaction request
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="interaction">Interaction</param>
        private void HandleInteraction(HttpListenerContext context, MessageInteraction interaction)
        {
            try
            {
                this.config.WriteLine($"Simulating message with description: {interaction.Description}");

                if (!this.Scenarios.Scenarios.TryGetValue(interaction.Description, out Scenario scenario))
                {
                    this.NotFoundResponse(context, $"WARNING: An interaction was not found for description: {interaction.Description}");
                    return;
                }

                JsonSerializerOptions settings = scenario.JsonSettings ?? this.defaultSettings;

                if (scenario.Metadata != null)
                {
                    string stringifyMetadata = JsonSerializer.Serialize(scenario.Metadata, settings);
                    string metadataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(stringifyMetadata));
                    context.Response.AddHeader("Pact-Message-Metadata", metadataBase64);

                    this.config.WriteLine($"Metadata: {stringifyMetadata}");
                }

                dynamic content = scenario.Invoke();
                string response = JsonSerializer.Serialize(content, settings);
                this.OkResponse(context, response);

                this.config.WriteLine($"Successfully simulated message with description: {interaction.Description}");
            }
            catch (Exception e)
            {
                this.ErrorResponse(context, $"ERROR: Error handling message interaction: {e}");
            }
        }

        /// <summary>
        /// Send a 400 Bad Request response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="message">Error message</param>
        private void BadRequestResponse(HttpListenerContext context, string message)
            => this.WriteOutput(context.Response, HttpStatusCode.BadRequest, message, "text/plain");

        /// <summary>
        /// Send a 404 Not Found response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="message">Error message</param>
        private void NotFoundResponse(HttpListenerContext context, string message)
            => this.WriteOutput(context.Response, HttpStatusCode.NotFound, message, "text/plain");

        /// <summary>
        /// Send a 405 Not Allowed response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="message">Error message</param>
        private void NotAllowedResponse(HttpListenerContext context, string message)
            => this.WriteOutput(context.Response, HttpStatusCode.MethodNotAllowed, message, "text/plain");

        /// <summary>
        /// Send a 500 Internal Server Error response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="message">Error message</param>
        private void ErrorResponse(HttpListenerContext context, string message)
            => this.WriteOutput(context.Response, HttpStatusCode.InternalServerError, message, "text/plain");

        /// <summary>
        /// Send a 200 OK response
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="message">Response body</param>
        private void OkResponse(HttpListenerContext context, string message)
            => this.WriteOutput(context.Response, HttpStatusCode.OK, message, "application/json");

        /// <summary>
        /// Write the response
        /// </summary>
        /// <param name="response">Response context</param>
        /// <param name="status">Status code</param>
        /// <param name="body">Response body</param>
        /// <param name="contentType">Content type</param>
        private void WriteOutput(HttpListenerResponse response, HttpStatusCode status, string body, string contentType)
        {
            this.config.WriteLine($"Body: {body}");

            try
            {
                response.StatusCode = (int)status;

                byte[] bytes = Encoding.UTF8.GetBytes(body);

                response.ContentType = contentType;
                response.ContentLength64 = bytes.Length;

                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                this.config.WriteLine($"ERROR: Unable to write response body: {e}");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            try
            {
                this.server?.Stop();
                this.server?.Close();
            }
            catch
            {
                // ignore - we're shutting down anyway
            }
        }
    }
}
