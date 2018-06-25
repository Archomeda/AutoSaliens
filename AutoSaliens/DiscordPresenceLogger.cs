using System;
using AutoSaliens.Console;
using DiscordRPC.Logging;

namespace AutoSaliens
{
    internal class DiscordPresenceLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public void Info(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info)
                return;
            Shell.WriteLine($"{{inf}}{message}", args);
        }

        public void Warning(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning)
                return;
            Shell.WriteLine($"{{warn}}{message}", args);
        }

        public void Error(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning && this.Level != LogLevel.Error)
                return;
            Shell.WriteLine($"{{err}}{message}", args);
        }
    }
}
