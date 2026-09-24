namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>Saves the user's INI edits: archive <c>Current</c>, write <c>zoo.ini</c> atomically, update <c>Current</c> — merged onto the file as it is on disk (SDD §7.3.2, §8.2).</summary>
/// <param name="InstallationId">The installation's identifier.</param>
/// <param name="Edits">The edited keys and their new raw values; an empty value clears the key (<c>key=</c>).</param>
public sealed record SaveIniCommand(Guid InstallationId, IReadOnlyDictionary<IniKeyId, string> Edits) : ICommand<ErrorOr<IniConfigResult>>;
