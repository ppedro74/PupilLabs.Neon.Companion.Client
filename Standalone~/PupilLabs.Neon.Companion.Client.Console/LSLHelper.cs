namespace PupilLabs.Neon.Companion.Client.ConsoleApp
{
    using System.Diagnostics;
    using SharpLSL;

    internal static class LSLHelper
    {
        public static void GetLocalClock()
        {
            for (int i = 0; i < 10; ++i)
            {
                var stopwatch = Stopwatch.StartNew();
                var timestamp = LSL.GetLocalClock();
                var elapsedTime = stopwatch.Elapsed;

                stopwatch = Stopwatch.StartNew();
                var timestamp2 = Stopwatch.GetTimestamp();
                var elapsedTime2 = stopwatch.Elapsed;

                Console.WriteLine($"{timestamp:F7} {elapsedTime} <- LSL.GetLocalClock()");
                Console.WriteLine($"{TimestampToSeconds(timestamp2):F7} {elapsedTime2} <- TimestampToSeconds(Stopwatch.GetTimestamp())");
            }

            Console.WriteLine($"Is high resolution? {Stopwatch.IsHighResolution}.");
            Console.WriteLine($"Ticks per second: {TimeSpan.TicksPerSecond}.");
            Console.WriteLine($"Frequency: {Stopwatch.Frequency}.");
            Console.WriteLine($"Tick frequency: {(double)TimeSpan.TicksPerSecond / Stopwatch.Frequency}.");
        }

        public static void ListAllStreams()
        {
            UnityEngine.Debug.Log("Listing all LSL streams...");

            try
            {
                Console.WriteLine("Here is a one-shot resolve of all current streams:");

                // Discover all streams on the network
                var streamInfos = LSL.Resolve();

                var foundStreams = new Dictionary<string, StreamInfo>();
                foreach (var streamInfo in streamInfos)
                {
                    foundStreams[streamInfo.Uid] = streamInfo;
                    Console.WriteLine(streamInfo.ToXML());
                    Console.WriteLine();
                }

                Console.WriteLine("Press any key to switch to the continuous resolver test.");
                Console.ReadKey();

                using (var continuousResolver = new ContinuousResolver())
                {
                    while (true)
                    {
                        var resolvedStreams = continuousResolver.Results();
                        foreach (var streamInfo in resolvedStreams)
                        {
                            var streamUid = streamInfo.Uid;
                            if (!foundStreams.ContainsKey(streamUid))
                            {
                                foundStreams[streamUid] = streamInfo;
                                Console.WriteLine($"Found {streamInfo.Name}@{streamInfo.HostName}");
                            }
                        }

                        var missingStreams = foundStreams.Values.Where(streamInfo => resolvedStreams.All(resolvedStreamInfo => resolvedStreamInfo.Uid != streamInfo.Uid));
                        foreach (var streamInfo in missingStreams)
                        {
                            Console.WriteLine($"Lost {streamInfo.Name}@{streamInfo.HostName}");
                            foundStreams.Remove(streamInfo.Uid);
                        }

                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got an exception: {ex}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        static double TimestampToSeconds(long timestamp)
        {
#if false
            var tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
            return timestamp * tickFrequency / TimeSpan.TicksPerSecond;
#else
            return (double)timestamp / Stopwatch.Frequency;
#endif
        }

        static double TimestampToMilliseconds(long timestamp)
        {
            var tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
            return timestamp * tickFrequency / TimeSpan.TicksPerMillisecond;
        }


    }
}