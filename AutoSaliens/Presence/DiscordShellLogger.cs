using DiscordRPC.Logging;
using ILoggerDiscord = DiscordRPC.Logging.ILogger;

namespace AutoSaliens.Presence
{
    internal class DiscordShellLogger : ILoggerDiscord
    {
        public LogLevel Level { get; set; }

        public ILogger Logger { get; set; }


        public void Info(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info)
                return;
            this.Logger?.LogMessage($"{{inf}}[DISCORD] {message}", args);
        }

        public void Warning(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning)
                return;
            this.Logger?.LogMessage($"{{warn}}[DISCORD] {message}", args);
        }

        public void Error(string message, params object[] args)
        {
            if (this.Level != LogLevel.Info && this.Level != LogLevel.Warning && this.Level != LogLevel.Error)
                return;
            this.Logger?.LogMessage($"{{err}}[DISCORD] {message}", args);
        }
    }
}
