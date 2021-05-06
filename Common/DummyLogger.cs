using System;
using Microsoft.Extensions.Logging;

namespace Shine.Common
{
    public sealed class DummyLogger<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public DummyLogger(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger(typeof(T).Name);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);

        public bool IsEnabled(LogLevel logLevel)
            => _logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
            => _logger.BeginScope(state);
    }
}