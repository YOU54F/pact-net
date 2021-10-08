using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PactNet.Verifier.ProviderState;

namespace PactNet.AspNetCore.ProviderState
{
    /// <summary>
    /// Defines the provider state middleware
    /// It provides an endpoint to trigger the provider states before verification of pact interaction
    /// </summary>
    public class ProviderStateMiddleware
    {
        /// <summary>
        /// The next pointer
        /// </summary>
        private readonly RequestDelegate next;
        
        /// <summary>
        /// The provider state middleware options
        /// </summary>
        private readonly ProviderStateOptions options;

        /// <summary>
        /// The provider state accessor
        /// </summary>
        private readonly IProviderStateAccessor providerStateAccessor;

        /// <summary>
        /// Creates an instance of <see cref="ProviderStateMiddleware"/>
        /// </summary>
        /// <param name="options">the middleware options</param>
        /// <param name="providerStateAccessor">the provider state accessor</param>
        /// <param name="next">the next handle</param>
        public ProviderStateMiddleware(IOptions<ProviderStateOptions> options, IProviderStateAccessor providerStateAccessor, RequestDelegate next)
        {
            this.options = options.Value;
            this.providerStateAccessor = providerStateAccessor;
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!(context.Request.Path.Value?.StartsWith(options.RouteProviderState) ?? false))
            {
                await this.next(context);
                return;
            }

            if (context.Request.Method != HttpMethod.Post.ToString())
            {
                await WriteErrorInResponseAsync(context, "The provider state invocation needs to be a post method. Check on your verifier side if the provider state url is nicely configured.", HttpStatusCode.BadRequest);
                return;
            }

            var providerStateBody = await GetProviderStateInteraction(context);

            IProviderState providerState = this.providerStateAccessor.GetByDescription(providerStateBody.Name);

            if (providerState == null)
            {
                await WriteErrorInResponseAsync(context, "The provider state cannot be invoked because it has not been register in the provider test correctly.", HttpStatusCode.NotFound);
                return;
            }

            providerState.Execute();

            context.Response.StatusCode = (int)HttpStatusCode.OK;

            await WriteToResponseAsync(context, string.Empty);
        }

        /// <summary>
        /// Read the request body as a provider state interaction
        /// </summary>
        /// <param name="context">The http context</param>
        /// <returns>The request body</returns>
        private static async Task<ProviderStateInteraction> GetProviderStateInteraction(HttpContext context)
        {
            string jsonRequestBody;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                jsonRequestBody = await reader.ReadToEndAsync();
            }

            ProviderStateInteraction providerStateBody =
                JsonConvert.DeserializeObject<ProviderStateInteraction>(jsonRequestBody);
            return providerStateBody;
        }

        /// <summary>
        /// Write a dynamic message to the http response
        /// </summary>
        /// <param name="context">the http context</param>
        /// <param name="response">the dynamic message</param>
        private static Task WriteToResponseAsync(HttpContext context, dynamic response)
        {
            string responseBody = JsonConvert.SerializeObject(response);
            return context.Response.WriteAsync(responseBody);
        }

        /// <summary>
        /// Write an error message in response
        /// </summary>
        /// <param name="context">The http context</param>
        /// <param name="errorMessage">The error message in the response content</param>
        /// <param name="statusCodeError">The status code related to the error</param>
        private static async Task WriteErrorInResponseAsync(HttpContext context, string errorMessage, HttpStatusCode statusCodeError)
        {
            context.Response.StatusCode = (int)statusCodeError;
            await WriteToResponseAsync(context, errorMessage);
        }
    }
}
