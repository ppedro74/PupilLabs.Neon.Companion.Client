namespace PupilLabs.Neon.Companion.Client.ConsoleApp
{
    using CommandLine;

    internal static class Program
    {
        private sealed class Options
        {
            [Option("run-get-status", Required = false, Default = false, HelpText = "Fetch the Neon status before running time echo.")]
            public bool RunGetStatus { get; set; }

            [Option("neon-ip", Required = false, Default = "neon.local", HelpText = "The Neon host name or IP address.")]
            public string NeonIp { get; set; } = "neon.local";

            [Option("neon-port", Required = false, Default = 8080, HelpText = "The Neon API port.")]
            public int NeonPort { get; set; } = 8080;

            [Option("run-time-echo", Required = false, Default = false, HelpText = "Run the time echo estimate.")]
            public bool RunTimeEcho { get; set; }

            [Option("time-echo-port", Required = false, HelpText = "Optional time echo port. If omitted, it is read from the Neon status.")]
            public int? TimeEchoPort { get; set; }
        }

        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunAsync);
        }

        private static async Task RunAsync(Options options)
        {
            var neonIp = options.NeonIp;
            var neonPort = options.NeonPort;
            var runGetStatus = options.RunGetStatus;
            var runTimeEcho = options.RunTimeEcho;
            var timeEchoPort = options.TimeEchoPort;

            if (!NetworkHelper.IsIpAddress(neonIp))
            {
                var result = await NetworkHelper.ResolveHostToIpAsync(neonIp);
                if (result.IsSuccess)
                {
                    neonIp = result.Value;
                }
                else
                {
                    Console.Error.WriteLine($"Error: Failed to resolve neonIp=['{neonIp}'] ErrorMessage=[{result.ErrorMessage}]");
                    return;
                }
            }

            if (runGetStatus)
            {
                if (string.IsNullOrEmpty(neonIp))
                {
                    Console.Error.WriteLine($"Error: NeonIp cannot be null.");
                    return;
                }
                
                var (success, statusTimeEchoPort) = await NeonHelper.GetStatusAsync(neonIp, neonPort, new CancellationTokenSource(TimeSpan.FromSeconds(45)).Token);
                if (success && timeEchoPort == null)
                {
                    timeEchoPort = statusTimeEchoPort;
                }
            }

            if (runTimeEcho)
            {
                if (timeEchoPort == null)
                {
                    Console.WriteLine("Error: TimeEchoPort parameter not set.");
                }
                else
                {
                    var result = await NeonHelper.GetEstimateTimeOffsetAsync(neonIp, timeEchoPort.Value, 25, 20, new CancellationTokenSource(TimeSpan.FromSeconds(45)).Token);
                }
            }
        }
    }
}