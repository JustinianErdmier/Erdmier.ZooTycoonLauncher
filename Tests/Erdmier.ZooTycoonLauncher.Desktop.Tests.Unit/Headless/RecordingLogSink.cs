using System.Text.RegularExpressions;

using Avalonia.Logging;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Headless;

/// <summary>
///     An Avalonia <see cref="ILogSink" /> that keeps every warning and error it receives, so a headless test can fail on a problem Avalonia reports and recovers from rather
///     than throws, such as a binding that cannot convert its value.
/// </summary>
public sealed class RecordingLogSink : ILogSink
{
    private static readonly Regex Placeholder = new(@"\{\w+\}");

    private readonly List<string> _messages = [];

    private readonly Lock _gate = new();

    /// <summary>The formatted warnings and errors recorded since the last <see cref="Clear" />.</summary>
    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_gate)
            {
                return [ .. _messages ];
            }
        }
    }

    /// <inheritdoc />
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Record(level, area, source, messageTemplate, propertyValues: []);

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        => Record(level, area, source, messageTemplate, propertyValues);

    /// <summary>Forgets every recorded message.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
        }
    }

    private void Record(LogEventLevel level, string area, object? source, string messageTemplate, object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        // Avalonia fills its templates positionally, so the nth {Placeholder} takes the nth value.
        int next = 0;

        string text = Placeholder.Replace(messageTemplate, match => next < propertyValues.Length ? $"{propertyValues[ next++ ]}" : match.Value);

        string message = $"[{level}] [{area}] {source?.GetType().Name}: {text}";

        lock (_gate)
        {
            _messages.Add(message);
        }
    }
}
