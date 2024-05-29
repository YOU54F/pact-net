using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Consumer.Models;

namespace Consumer
{
    public class EventsApiClient : IDisposable
    {
        private readonly HttpClient httpClient;

        public EventsApiClient(Uri baseUri, string authToken = null)
        {
            this.httpClient = new HttpClient { BaseAddress = baseUri };

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            }
        }

        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public async Task<HttpStatusCode> UploadFile(FileInfo file)
        {

            using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

            var request = new MultipartFormDataContent();
            request.Headers.ContentType.MediaType = "multipart/form-data";

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            var fileName = file.Name;
            var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            var encodedFileName = Convert.ToBase64String(fileNameBytes);
            request.Add(fileContent, "file", fileName);
            request.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = fileName,
                FileNameStar = $"utf-8''{encodedFileName}"
            };

            HttpResponseMessage response = await this.httpClient.PostAsync("/events/upload-file", request);
            try
            {
                var statusCode = response.StatusCode;
                if (statusCode == HttpStatusCode.Created)
                {
                    return statusCode;
                }
                throw new HttpRequestException(
               string.Format("The Events API request for POST /upload-file failed. Response Status: {0}, Response Body: {1}",
               response.StatusCode,
               await response.Content.ReadAsStringAsync()));
            }
            finally
            {
                Dispose(request, response);
            }
        }
        private static async Task RaiseResponseError(HttpRequestMessage failedRequest, HttpResponseMessage failedResponse)
        {
            throw new HttpRequestException(
                string.Format("The Events API request for {0} {1} failed. Response Status: {2}, Response Body: {3}",
                failedRequest.Method.ToString().ToUpperInvariant(),
                failedRequest.RequestUri,
                (int)failedResponse.StatusCode,
                await failedResponse.Content.ReadAsStringAsync()));
        }

        public void Dispose()
        {
            Dispose(this.httpClient);
        }

        public void Dispose(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables.Where(d => d != null))
            {
                disposable.Dispose();
            }
        }
    }
}
