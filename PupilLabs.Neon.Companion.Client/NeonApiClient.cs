#nullable enable
namespace PupilLabs.Neon.Companion.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using PupilLabs.Neon.Companion.Client.Models;

    public sealed class NeonApiClient : INeonApiClient
    {
        private readonly HttpClient httpClient;
        private readonly JsonSerializerSettings serializerSettings;
        private readonly bool ownsHttpClient;

        public NeonApiClient(HttpClient httpClient, JsonSerializerSettings? serializerSettings = null)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.serializerSettings = serializerSettings ?? CreateDefaultSettings();

            if (this.httpClient.BaseAddress is null)
            {
                throw new ArgumentException("HttpClient.BaseAddress must be configured and include the API host.", nameof(httpClient));
            }

            var normalizedBaseAddress = NormalizeApiBaseAddress(this.httpClient.BaseAddress);
            if (normalizedBaseAddress != this.httpClient.BaseAddress)
            {
                this.httpClient.BaseAddress = normalizedBaseAddress;
            }
        }

        private NeonApiClient(HttpClient httpClient, bool ownsHttpClient, JsonSerializerSettings? serializerSettings = null)
            : this(httpClient, serializerSettings)
        {
            this.ownsHttpClient = ownsHttpClient;
        }

        public static NeonApiClient Create(Uri serverUri, HttpMessageHandler? handler = null, JsonSerializerSettings? serializerSettings = null)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            var client = handler is null ? new HttpClient() : new HttpClient(handler);
            client.BaseAddress = NormalizeApiBaseAddress(serverUri);

            return new NeonApiClient(client, ownsHttpClient: true, serializerSettings);
        }

        public Task<NeonResult<ApiEnvelope<IReadOnlyList<StatusUpdate>>>> GetStatusAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<IReadOnlyList<StatusUpdate>>(HttpMethod.Get, "status", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<RecordingStart>>> StartRecordingAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<RecordingStart>(HttpMethod.Post, "recording:start", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<RecordingStop>>> StopAndSaveRecordingAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<RecordingStop>(HttpMethod.Post, "recording:stop_and_save", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<RecordingCancel>>> CancelRecordingAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<RecordingCancel>(HttpMethod.Post, "recording:cancel", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<RecordingEvent>>> CreateEventAsync(EventPost request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return this.SendAsync<RecordingEvent>(HttpMethod.Post, "event", request, cancellationToken);
        }

        public Task<NeonResult<ApiEnvelope<IReadOnlyList<Template>>>> GetTemplateDefinitionAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<IReadOnlyList<Template>>(HttpMethod.Get, "template_def", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<TemplateData>>> GetTemplateDataAsync(CancellationToken cancellationToken = default)
            =>
                this.SendAsync<TemplateData>(HttpMethod.Get, "template_data", null, cancellationToken);

        public Task<NeonResult<ApiEnvelope<TemplateData>>> SetTemplateDataAsync(TemplateData request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return this.SendAsync<TemplateData>(HttpMethod.Post, "template_data", request, cancellationToken);
        }

        public static bool TryDeserializeStatusData<TData>(StatusUpdate statusUpdate, out TData? value)
        {
            value = default;
            if (statusUpdate?.Data == null || statusUpdate.Data.Type == Newtonsoft.Json.Linq.JTokenType.Null)
            {
                return false;
            }

            try
            {
                value = statusUpdate.Data.ToObject<TData>(JsonSerializer.Create(CreateDefaultSettings()));
                return value is not null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<NeonResult<ApiEnvelope<TResponse>>> SendAsync<TResponse>(HttpMethod method, string path, object? requestBody, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);

                if (requestBody is not null)
                {
                    var jsonPayload = JsonConvert.SerializeObject(requestBody, this.serializerSettings);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                }

                // Removed .ConfigureAwait(false) to respect Unity's main thread SynchronizationContext upon return
                using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseText = string.Empty;
                    if (response.Content != null)
                    {
                        responseText = await response.Content.ReadAsStringAsync();
                    }

                    return NeonResult<ApiEnvelope<TResponse>>.Failure($"Request failed with status code: {response.StatusCode}", response.StatusCode, responseText);
                }

                if (response.Content == null)
                {
                    return NeonResult<ApiEnvelope<TResponse>>.Failure("Server response content was null.", response.StatusCode);
                }

                // Stream-based deserialization avoids massive memory string allocations
                using var stream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(streamReader);

                var serializer = JsonSerializer.Create(this.serializerSettings);
                var envelope = serializer.Deserialize<ApiEnvelope<TResponse>>(jsonReader);

                if (envelope is null)
                {
                    return NeonResult<ApiEnvelope<TResponse>>.Failure("Failed to deserialize JSON response payload.", response.StatusCode);
                }

                return NeonResult<ApiEnvelope<TResponse>>.Success(envelope, response.StatusCode);
            }
            catch (OperationCanceledException)
            {
                return NeonResult<ApiEnvelope<TResponse>>.Failure("The network task execution operation was canceled.");
            }
            catch (Exception ex)
            {
                return NeonResult<ApiEnvelope<TResponse>>.Failure($"Unhandled exceptions captured: {ex.Message}");
            }
        }

        private static JsonSerializerSettings CreateDefaultSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }

        private static Uri NormalizeApiBaseAddress(Uri serverUri)
        {
            if (!serverUri.IsAbsoluteUri)
            {
                throw new ArgumentException("The server URI must be absolute.", nameof(serverUri));
            }

            var builder = new UriBuilder(serverUri);
            var path = builder.Path.TrimEnd('/');

            if (!path.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                path = string.IsNullOrWhiteSpace(path) || path == "/" ? "/api" : $"{path}/api";
            }

            builder.Path = $"{path}/";
            return builder.Uri;
        }

        public void Dispose()
        {
            if (this.ownsHttpClient)
            {
                this.httpClient.Dispose();
            }
        }
    }
}