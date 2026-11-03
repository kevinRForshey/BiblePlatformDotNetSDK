using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Platform.API.Tests.Fakes;

/// <summary>
/// An <see cref="ILogger{TCategoryName}"/> that records every formatted log message, so tests
/// can assert on what was logged instead of discarding it via <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/>.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
