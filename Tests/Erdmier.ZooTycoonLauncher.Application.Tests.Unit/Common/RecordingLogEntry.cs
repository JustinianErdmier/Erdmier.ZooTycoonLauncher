namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Common;

/// <summary>One entry captured by a <see cref="RecordingLogger{T}" />.</summary>
/// <param name="Level">The entry's log level.</param>
/// <param name="Message">The formatted message.</param>
internal sealed record RecordingLogEntry(LogLevel Level, string Message);
