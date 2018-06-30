using System;

namespace AutoSaliens.Console
{
    internal class ConsoleLogger : ILogger
    {
        public void LogMessage(string message, params object[] args) =>
            this.LogMessage(message, true, args);

        public void LogMessage(string message, bool includeTime, params object[] args)
        {
            if (includeTime)
                message = $"{{logtime}}[{DateTime.Now.ToString("dd HH:mm:ss.fff")}]{{reset}} {message}";
            Shell.WriteLine(message, args);
        }

        public void LogCommandOutput(string message) =>
            LogMessage(message, false);

        public void LogException(Exception ex) =>
            LogMessage($"An error has occured: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }
}
