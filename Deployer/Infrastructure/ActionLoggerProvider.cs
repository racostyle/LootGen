using Microsoft.Extensions.Logging;

namespace Deployer.Infrastructure
{
    public sealed class ActionLoggerProvider : ILoggerProvider
    {
        private readonly UiLogSink _sink;

        public ActionLoggerProvider(UiLogSink sink)
        {
            _sink = sink;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ActionLogger(categoryName, _sink);
        }

        public void Dispose()
        {
        }

        private sealed class ActionLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly UiLogSink _sink;

            public ActionLogger(string categoryName, UiLogSink sink)
            {
                _categoryName = categoryName;
                _sink = sink;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var shortName = _categoryName;
                var lastDot = shortName.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < shortName.Length - 1)
                {
                    shortName = shortName[(lastDot + 1)..];
                }

                var message = $"{DateTime.Now:HH:mm:ss} [{logLevel}] {shortName}: {formatter(state, exception)}";
                if (exception is not null)
                {
                    message += Environment.NewLine + exception;
                }

                _sink.Write(message);
            }
        }
    }
}
