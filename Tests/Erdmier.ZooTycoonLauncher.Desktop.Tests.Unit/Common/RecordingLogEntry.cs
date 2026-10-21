namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Common;

/// <summary>One entry captured by a <see cref="RecordingLogger{T}" />.</summary>
/// <param name="Level">The entry's log level.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The logged exception, or <see langword="null" />.</param>
internal sealed record RecordingLogEntry(LogLevel Level, string Message, Exception? Exception);
