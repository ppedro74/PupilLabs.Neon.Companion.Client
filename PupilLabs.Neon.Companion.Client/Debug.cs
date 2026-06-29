#if !UNITY_5_3_OR_NEWER

#pragma warning disable IDE0130
namespace UnityEngine
#pragma warning restore IDE0130
{
    using System;
    using System.IO;

    public static class Debug
    {
        private static readonly object FileLock = new object();
        private static readonly bool IsInteractiveConsole = DetermineIfConsoleExists();

        public static void Log(object message)
        {
            WriteLog("INFO", message?.ToString());
        }

        public static void LogWarning(object message)
        {
            WriteLog("WARN", message?.ToString());
        }

        public static void LogError(object message)
        {
            WriteLog("ERROR", message?.ToString());
        }

        public static void LogException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            WriteLog("EXCEPT", $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
        }

        private static void WriteLog(string level, string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            if (IsInteractiveConsole)
            {
                var originalColor = Console.ForegroundColor;
                if (level == "WARN")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (level == "ERROR" || level == "EXCEPT")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine(formattedMessage);
                Console.ForegroundColor = originalColor;
            }
            else
            {
                // Headless environment / Web Project: Fallback safely to a rolling temp file
                WriteToTempFile(formattedMessage);
            }
        }

        private static void WriteToTempFile(string message)
        {
            try
            {
                var fileName = $"PupilLabs.Neon.Companion.Client.{DateTime.UtcNow:yyyy-MM-dd}.log";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);

                // Thread-safe append lock to prevent multi-threaded Web requests from colliding
                lock (FileLock)
                {
                    File.AppendAllText(tempPath, message + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private static bool DetermineIfConsoleExists()
        {
            try
            {
                // In headless Linux environments, Docker, or ASP.NET web hosts, 
                // accessing WindowHeight or checking redirection states will throw or evaluate to false/zero.
                if (Console.IsOutputRedirected)
                {
                    return false;
                }

                return Console.WindowHeight > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif