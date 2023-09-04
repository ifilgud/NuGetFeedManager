using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NuGet.Common;

namespace NuGetFeedManager
{
    //
    public class Logger : ILogger
    {
        private readonly ObservableCollection<LogEntry> _logs;

        public Logger(ObservableCollection<LogEntry> logs)
        {
            _logs = logs;
        }

        public void LogDebug(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Debug));
        }

        public void LogVerbose(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Verbose));
        }

        public void LogInformation(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Information));
        }

        public void LogMinimal(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Minimal));
        }

        public void LogWarning(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Warning));
        }

        public void LogError(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Error));
        }

        public void LogInformationSummary(string data)
        {
            _logs.Add(new LogEntry(data, DateTimeOffset.Now, LogSeverity.Summary));
        }

        public void Log(LogLevel level, string data)
        {
            switch (level)
            {
                case LogLevel.Error:
                    LogError(data);
                    break;
                case LogLevel.Warning:
                    LogWarning(data);
                    break;
                case LogLevel.Verbose:
                    LogVerbose(data);
                    break;
                case LogLevel.Minimal:
                    LogMinimal(data);
                    break;
                case LogLevel.Debug:
                    LogDebug(data);
                    break;
                default:
                    LogInformation(data);
                    break;
            }
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
