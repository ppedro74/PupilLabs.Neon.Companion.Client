namespace PupilLabs.Neon.Companion.Client
{
    using System;
    using System.Diagnostics;

    public static class HighResTime
    {
        private static readonly double nsPerTick = 1_000_000_000.0 / Stopwatch.Frequency;
        private static readonly long epochNs;
        private static readonly long startTicks;

        static HighResTime()
        {
            // Capture the epoch time once
            epochNs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;

            // Capture the high‑resolution timer start
            startTicks = Stopwatch.GetTimestamp();
        }

        public static long TimeNs()
        {
            var ticks = Stopwatch.GetTimestamp() - startTicks;
            var nsSinceStart = (long)(ticks * nsPerTick);
            return epochNs + nsSinceStart;
        }

        public static long TimeMs()
        {
            var ticks = Stopwatch.GetTimestamp() - startTicks;
            var nsSinceStart = (long)(ticks * nsPerTick);
            var msSinceStart = nsSinceStart / 1_000_000;
            return (epochNs / 1_000_000) + msSinceStart;
        }
    }
}