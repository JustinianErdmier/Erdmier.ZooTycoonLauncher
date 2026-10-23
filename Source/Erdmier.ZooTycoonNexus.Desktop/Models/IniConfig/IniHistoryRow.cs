namespace Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;

/// <summary>One line of the Restore Previous INI history list.</summary>
/// <param name="Entry">The history entry the line represents.</param>
/// <param name="CapturedText">When the snapshot was captured, in local time (<c>d MMM yyyy HH:mm</c>, en-GB).</param>
/// <param name="TriggerText">What triggered the capture (the trigger's name).</param>
/// <param name="DeltaText">How many settings differ from the current values.</param>
public sealed record IniHistoryRow(IniHistoryEntry Entry, string CapturedText, string TriggerText, string DeltaText);