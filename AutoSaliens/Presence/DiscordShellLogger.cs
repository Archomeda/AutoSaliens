using AutoSaliens.Console;
using DiscordRPC.Logging;

namespace AutoSaliens.Presence
{
    internal class DiscordShellLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public void Info(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info)
                return;
            Shell.WriteLine($"{{inf}}[DISCORD] {message}", args);
        }

        public void Warning(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning)
                return;
            Shell.WriteLine($"{{warn}}[DISCORD] {message}", args);
        }

        public void Error(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning && this.Level != LogLevel.Error)
                return;
            Shell.WriteLine($"{{err}}[DISCORD] {message}", args);
        }
    }
}
