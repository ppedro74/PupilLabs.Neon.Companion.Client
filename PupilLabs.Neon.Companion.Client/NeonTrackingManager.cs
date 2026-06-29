#nullable enable
namespace PupilLabs.Neon.Companion.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public class NeonTrackingManager
    {
        private readonly INeonApiClient apiClient;
        private CancellationTokenSource? actionCancellationSource;

        public NeonTrackingManager(INeonApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        // High performance callback fire-and-forget pattern
        public void RequestStartRecording(Action<bool, string> onComplete)
        {
            // Cancel prior pending requests if user multi-clicks UI elements
            this.actionCancellationSource?.Cancel();
            this.actionCancellationSource = new CancellationTokenSource();

            // Fire the underlying task to execute entirely in the background
            _ = this.ExecuteStartRecordingAsync(onComplete, this.actionCancellationSource.Token);
        }

        private async Task ExecuteStartRecordingAsync(Action<bool, string> onComplete, CancellationToken token)
        {
            Debug.Log("Initiating Neon Eye-Tracker Recording request...");

            var result = await this.apiClient.StartRecordingAsync(token);

            // Safe execution context wrapper - checks success cleanly without catching raw exceptions
            if (result.IsSuccess && result.Value?.Result != null)
            {
                var recordingId = result.Value.Result.Id;
                onComplete?.Invoke(true, $"Recording successfully initiated! ID: {recordingId}");
            }
            else
            {
                onComplete?.Invoke(false, $"Failed to start recording context: {result.ErrorMessage}");
            }
        }

        public void CancelActiveOperations()
        {
            this.actionCancellationSource?.Cancel();
        }
    }
}