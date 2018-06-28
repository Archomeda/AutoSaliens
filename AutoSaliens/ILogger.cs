using System;

namespace AutoSaliens
{
    internal interface ILogger
    {
        void LogMessage(string message, params object[] args);

        void LogMessage(string message, bool includeTime, params object[] args);

        void LogCommandOutput(string message);

        void LogException(Exception ex);
    }
}
