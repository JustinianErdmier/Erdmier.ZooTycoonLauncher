namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Common;

/// <summary>A test <see cref="ILogger{TCategoryName}" /> that records every entry's level and formatted message.</summary>
/// <typeparam name="T">The logging category type.</typeparam>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<RecordingLogEntry> _entries = [];

    /// <summary>Gets the entries logged so far, in order.</summary>
    public IReadOnlyList<RecordingLogEntry> Entries => _entries;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _entries.Add(new RecordingLogEntry(logLevel, formatter(state, exception)));
}
