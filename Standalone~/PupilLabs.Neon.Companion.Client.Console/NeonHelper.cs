namespace PupilLabs.Neon.Companion.Client.ConsoleApp
{
    using PupilLabs.Neon.Companion.Client.Models;

    internal static class NeonHelper
    {
        public static async Task<(bool success, int? timeEchoPort)> GetStatusAsync(string companionDeviceIp, int companionHttpPort, CancellationToken cancellationToken)
        {
            var baseUri = new Uri($"http://{companionDeviceIp}:{companionHttpPort}");

            Console.WriteLine($"Executing {nameof(GetStatusAsync)} baseUri=[{baseUri}]");

            using var httpClient = new HttpClient { BaseAddress = baseUri };

            using (var neonClient = new NeonApiClient(httpClient))
            {
                var statusResult = await neonClient.GetStatusAsync(cancellationToken);

                if (statusResult.IsSuccess && statusResult.Value?.Result != null)
                {
                    Console.WriteLine($"...Success: ServerMessage=[{statusResult.Value.Message}]");

                    Console.WriteLine("......Status Updates:");
                    foreach (var update in statusResult.Value.Result)
                    {
                        Console.WriteLine($".........Model=[{update.Model}] Data=[{update.Data.ToString(Newtonsoft.Json.Formatting.None)}]");
                    }

                    int? timeEchoPort = null;

                    var statusUpdate = statusResult.Value.Result.FirstOrDefault(x => string.Equals(x.Model, "Phone", StringComparison.OrdinalIgnoreCase));
                    if (statusUpdate != null && NeonApiClient.TryDeserializeStatusData<Phone>(statusUpdate, out var phone) && phone != null)
                    {
                        timeEchoPort = phone.TimeEchoPort;
                        Console.WriteLine($"......DeviceId=[{phone.DeviceId}] TimeEchoPort=[{phone.TimeEchoPort}]");
                    }

                    return (true, timeEchoPort);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"...Error: ErrorMessage=[{statusResult.ErrorMessage}] StatusCode=[{statusResult.StatusCode}]");
                    Console.WriteLine($"......Raw Error Output Stream: {statusResult.ResponseBody}");
                    Console.ResetColor();

                    return (false, null);
                }

            }
        }

        public static async Task<(bool success, long? timeEchoPort)> GetEstimateTimeOffsetAsync(string companionDeviceIp, int timeEchoPort, int sampleCount, int sampleIntervalMs, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Executing {nameof(GetEstimateTimeOffsetAsync)} ip=[{companionDeviceIp}] port=[{timeEchoPort}] sampleCount=[{sampleCount}], sampleIntervalMs=[{sampleIntervalMs}]");

            var timeOffsetResult = await TimeEchoClient.EstimateTimeOffsetAsync(
                host: companionDeviceIp,
                port: timeEchoPort,
                sampleCount: sampleCount,
                sampleIntervalMs: sampleIntervalMs,
                cancellationToken: cancellationToken
            );

            if (timeOffsetResult.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"...Success: CalculatedClockOffset=[{timeOffsetResult.Value}] ms");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"...Error: ErrorMessage=[{timeOffsetResult.ErrorMessage}]");
                Console.ResetColor();
            }

            return (timeOffsetResult.IsSuccess, timeOffsetResult.Value);
        }


/*
        public void Test1()
        {
            // Determine the hostname/IP and port from arguments or defaults
            string hostOrIp;
            int port;

            if (args.Length > 0)
            {
                // First argument is hostname or IP
                hostOrIp = args[0];
                // Second argument is port (optional, defaults to DefaultPort)
                port = args.Length > 1 && int.TryParse(args[1], out var parsedPort) ? parsedPort : DefaultPort;
            }
            else
            {
                hostOrIp = DefaultHostname;
                port = DefaultPort;
            }

            string resolvedIp;
            try
            {
                // Resolve hostname to IP (or validate IP)
                resolvedIp = await ResolveHostToIpAsync(hostOrIp);
                Console.WriteLine($"Resolved '{hostOrIp}' to IP: {resolvedIp}");
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"Hostname resolution error: {ex.Message}");
                return;
            }

            var serverUri = new Uri($"http://{resolvedIp}:{port}", UriKind.Absolute);

            try
            {
                using var client = NeonApiClient.Create(serverUri);
                var status = await client.GetStatusAsync();

                var phoneStatus = status?.Result?.FirstOrDefault(x => string.Equals(x.Model, "Phone", StringComparison.OrdinalIgnoreCase));

                if (phoneStatus is not null &&
                    NeonApiClient.TryDeserializeStatusData<Phone>(phoneStatus, out var phone) &&
                    phone?.TimeEchoPort is int timeEchoPort)
                {
                    Console.WriteLine($"TimeEchoPort: {timeEchoPort}");

                    timeEchoPort = 9090;

                    // Estimate time offset with the device using the resolved IP
                    var timeOffset = await TimeEchoClient.EstimateTimeOffset(resolvedIp, timeEchoPort, 2);
                    Console.WriteLine($"Estimated time offset: {timeOffset} ms");
                }

                Console.WriteLine($"Neon API reachable. Status items: {status?.Result?.Count ?? 0}");

                // Start recording
                Console.WriteLine("\nStarting recording...");
                var startResult = await client.StartRecordingAsync();

                if (startResult.Message is not null)
                {
                    Console.Error.WriteLine($"Recording start message: {startResult.Message}");
                }

                if (startResult.Result?.Id is Guid recordingId)
                {
                    Console.WriteLine($"✓ Recording started successfully. ID: {recordingId}");

                    // Wait 5 seconds
                    Console.WriteLine("Recording for 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    // Stop recording
                    Console.WriteLine("Stopping recording...");
                    var stopResult = await client.StopAndSaveRecordingAsync();

                    if (stopResult.Message is not null)
                    {
                        Console.WriteLine($"Recording stop message: {stopResult.Message}");
                    }

                    if (stopResult.Result?.Id is Guid stoppedId)
                    {
                        Console.WriteLine($"Recording stopped successfully. ID: {stoppedId}");

                        if (stopResult.Result.RecordingDurationNanoseconds is long durationNs)
                        {
                            var durationMs = durationNs / 1_000_000.0;
                            var durationSeconds = durationMs / 1000.0;
                            Console.WriteLine($"  Duration: {durationSeconds:F3} seconds ({durationMs:F2} ms)");
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("! Recording stop did not return an ID");
                    }
                }
                else
                {
                    Console.Error.WriteLine("! Recording start did not return an ID");
                }
            }
            catch (NeonApiException ex)
            {
                Console.Error.WriteLine($"Neon API error ({(int)ex.StatusCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
*/
    }
}