using System;

namespace NuGetFeedManager
{
    public class LogEntry
    {
        public string Message { get; }
        public DateTimeOffset Timestamp { get; }
        public LogSeverity Severity { get; }

        public LogEntry(string message, DateTimeOffset timestamp, LogSeverity severity)
        {
            Message = message;
            Timestamp = timestamp;
            Severity = severity;
        }
    }
}
