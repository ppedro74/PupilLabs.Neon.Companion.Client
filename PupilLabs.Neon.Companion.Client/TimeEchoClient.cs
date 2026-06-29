namespace PupilLabs.Neon.Companion.Client
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public static class TimeEchoClient
    {
        /// <summary>
        /// Orchestrates exactly sampleCount independent calls. 
        /// Breaks immediately if an execution step encounters a fatal socket error.
        /// </summary>
        public static async Task<NeonResult<long>> EstimateTimeOffsetAsync(
            string host,
            int port,
            int sampleCount = 100,
            int sampleIntervalMs = 0,
            CancellationToken cancellationToken = default)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than zero.");
            }

            if (sampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count must be greater than zero.");
            }

            if (sampleIntervalMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleIntervalMs), "Sample interval cannot be negative.");
            }

            long offsetSum = 0;
            var successfulIterations = 0;

            for (var i = 0; i < sampleCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Execute a completely self-contained network connection, write, and read cycle
                var sampleResult = await ExecuteSingleSampleAsync(host, port, cancellationToken);

                if (sampleResult.IsSuccess)
                {
                    offsetSum += sampleResult.Value;
                    successfulIterations++;
                }
                else
                {
                    Debug.LogWarning($"Sample transaction iteration {i} failed: {sampleResult.ErrorMessage}");

                    // If the failure was a connection/socket issue, abort the remaining calls immediately
                    if (sampleResult.ResponseBody == "SOCKET_ERROR")
                    {
                        Debug.LogError("Terminating loop early. Target host is completely unreachable or port is closed.");
                        break;
                    }
                }

                if (sampleIntervalMs > 0 && i < sampleCount - 1)
                {
                    try
                    {
                        await Task.Delay(sampleIntervalMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            if (successfulIterations == 0)
            {
                return NeonResult<long>.Failure("Zero successful time-offset metrics could be extracted from the tracking hardware.");
            }

            return NeonResult<long>.Success(offsetSum / successfulIterations, System.Net.HttpStatusCode.OK);
        }

        private static async Task<NeonResult<long>> ExecuteSingleSampleAsync(string host, int port, CancellationToken cancellationToken)
        {
            using var client = new TcpClient();

            // 1. Independent Connection Handshake
            try
            {
                var connectTask = client.ConnectAsync(host, port);
                if (cancellationToken.CanBeCanceled)
                {
                    var cancelTaskSource = new TaskCompletionSource<bool>();
                    using (cancellationToken.Register(() => cancelTaskSource.TrySetResult(true)))
                    {
                        if (await Task.WhenAny(connectTask, cancelTaskSource.Task) == cancelTaskSource.Task)
                        {
                            client.Close();
                            return NeonResult<long>.Failure("The operation connection sequence was canceled.", null, "SOCKET_ERROR");
                        }
                    }
                }

                await connectTask;
            }
            catch (SocketException ex)
            {
                return NeonResult<long>.Failure($"Socket connection failed to {host}:{port}: {ex.Message}", null, "SOCKET_ERROR");
            }
            catch (Exception ex)
            {
                return NeonResult<long>.Failure($"Unexpected connection failure: {ex.Message}", null, "SOCKET_ERROR");
            }

            // 2. Independent Data Write, Read, and Verification Phase
            try
            {
                using var stream = client.GetStream();

                const int tsSize = sizeof(long);
                const int responseSize = tsSize * 2;
                var response = new byte[responseSize];

                var beforeMs = HighResTime.TimeMs();
                var beforeBytes = NetworkBytesToLocal(BitConverter.GetBytes(beforeMs), 0, tsSize);

                // Write transaction payload
                await stream.WriteAsync(beforeBytes, 0, tsSize, cancellationToken);

                // Read incoming server payload frame blocks
                var read = 0;
                while (read < responseSize)
                {
                    var bytesRead = await stream.ReadAsync(response, read, responseSize - read, cancellationToken);
                    if (bytesRead == 0)
                    {
                        return NeonResult<long>.Failure("The remote time host closed the connection context prematurely.", null, "SOCKET_ERROR");
                    }

                    read += bytesRead;
                }

                var afterMs = HighResTime.TimeMs();

                var validationMs = BitConverter.ToInt64(NetworkBytesToLocal(response, 0, tsSize), 0);
                var serverMs = BitConverter.ToInt64(NetworkBytesToLocal(response, tsSize, tsSize), 0);

                if (validationMs != beforeMs)
                {
                    // Validation errors mean the network link works, but the data parsed was invalid. 
                    return NeonResult<long>.Failure($"Validation failed. Expected {beforeMs}, got {validationMs}", null, "DATA_ERROR");
                }

                var clientMidpoint = (beforeMs + afterMs) / 2;
                var calculatedOffset = clientMidpoint - serverMs;

                return NeonResult<long>.Success(calculatedOffset, System.Net.HttpStatusCode.OK);
            }
            catch (IOException ex)
            {
                return NeonResult<long>.Failure($"Network pipeline pipe dropped during payload exchange: {ex.Message}", null, "SOCKET_ERROR");
            }
            catch (Exception ex)
            {
                return NeonResult<long>.Failure($"Unexpected data mapping exception: {ex.Message}", null, "DATA_ERROR");
            }
        }

        private static byte[] NetworkBytesToLocal(byte[] networkBytes, int startIndex, int count)
        {
            var localBytes = networkBytes.AsSpan(startIndex, count).ToArray();

            if (BitConverter.IsLittleEndian)
            {
                localBytes.AsSpan().Reverse();
            }

            return localBytes;
        }
    }
}