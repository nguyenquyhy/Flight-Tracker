using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Clients.Logics
{
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, CustomLogger> loggers = new ConcurrentDictionary<string, CustomLogger>();
        private readonly LogLevel logLevel;
        private readonly Action<LogWrapper> callback;

        public CustomLoggerProvider(LogLevel logLevel, Action<LogWrapper> callback)
        {
            this.logLevel = logLevel;
            this.callback = callback;
        }

        public ILogger CreateLogger(string categoryName) => loggers.GetOrAdd(categoryName, name => new CustomLogger(name, logLevel, callback));

        public void Dispose()
        {
            loggers.Clear();
        }
    }

    public class CustomLogger : ILogger
    {
        private readonly string name;
        private readonly LogLevel logLevel;
        private readonly Action<LogWrapper> callback;

        public CustomLogger(string name, LogLevel logLevel, Action<LogWrapper> callback)
        {
            this.name = name;
            this.logLevel = logLevel;
            this.callback = callback;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            callback(new LogWrapper(name, logLevel, eventId, exception, formatter(state, exception)));
        }
    }

    public class LogWrapper
    {
        public string Name { get; set; }
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public Exception Exception { get; set; }
        public string FormattedString { get; set; }

        public LogWrapper(string name, LogLevel logLevel, EventId eventId, Exception exception, string formattedString)
        {
            Name = name;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            FormattedString = formattedString;
        }
    }
}
