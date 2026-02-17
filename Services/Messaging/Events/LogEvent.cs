using System;

namespace CryptoDayTraderSuite.Services.Messaging.Events
{
    public class LogEvent
    {
        public string Message { get; }
        public DateTime Timestamp { get; }
        public string Level { get; }

        public LogEvent(string message, string level = "INFO")
        {
            Message = message;
            Level = level;
            Timestamp = DateTime.UtcNow;
        }
    }
}
