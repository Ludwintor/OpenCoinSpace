using System;
using Microsoft.Extensions.Logging;

namespace OpenSpace.Logging
{
    internal sealed class ConsoleLogger : ILogger
    {
        private const int ID_LENGTH = 4;
        private const int NAME_LENGTH = 12;
        private const string TIMESTAMP_FORMAT = "HH:mm:ss zzz";

        private readonly LogLevel _minimumLevel;
        private readonly object _lock = new();

        internal ConsoleLogger(LogLevel minimumLevel)
        {
            _minimumLevel = minimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (_lock)
            {
                ReadOnlySpan<char> name = eventId.Name != null ? eventId.Name.AsSpan() : ReadOnlySpan<char>.Empty;
                name = name.Length > NAME_LENGTH ? name[..NAME_LENGTH] : name;
                Console.Write($"[{DateTimeOffset.Now.ToString(TIMESTAMP_FORMAT)}] [{eventId.Id,-ID_LENGTH}|{name,-NAME_LENGTH}] ");

                WriteLogLevel(logLevel);

                string message = formatter(state, exception);
                Console.WriteLine(message);
                if (exception != null)
                    Console.WriteLine(exception.ToString());
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        private static void WriteLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
            }

            Console.Write(logLevel switch
            {
                LogLevel.Trace => "[TRC]",
                LogLevel.Debug => "[DBG]",
                LogLevel.Information => "[INF]",
                LogLevel.Warning => "[WRN]",
                LogLevel.Error => "[ERR]",
                LogLevel.Critical => "[CRT]",
                _ => "[WTF]"
            });
            Console.ResetColor();
            Console.Write(' ');
        }
    }
}
