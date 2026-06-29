namespace PupilLabs.Neon.Companion.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using PupilLabs.Neon.Companion.Client.Models;

    public interface INeonApiClient : IDisposable
    {
        Task<NeonResult<ApiEnvelope<IReadOnlyList<StatusUpdate>>>> GetStatusAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<RecordingStart>>> StartRecordingAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<RecordingStop>>> StopAndSaveRecordingAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<RecordingCancel>>> CancelRecordingAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<RecordingEvent>>> CreateEventAsync(EventPost request, CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<IReadOnlyList<Template>>>> GetTemplateDefinitionAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<TemplateData>>> GetTemplateDataAsync(CancellationToken cancellationToken = default);
        Task<NeonResult<ApiEnvelope<TemplateData>>> SetTemplateDataAsync(TemplateData request, CancellationToken cancellationToken = default);
    }
}