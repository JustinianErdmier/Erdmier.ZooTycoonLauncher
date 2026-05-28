namespace Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;

/// <summary>
/// Implementation of <see cref="IInstallationLocator" /> that walks (in order): the persisted last-known directory, the two
/// hard-coded Program Files paths, then eight registry value-name variants under
/// <c>HKLM\SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0</c>. The first directory containing <c>zoo.exe</c> wins.
/// </summary>
public sealed class InstallationLocator : IInstallationLocator
{
	private const string ZooKeyPath = @"SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0";
	private const string ExeFileName = "zoo.exe";

	private static readonly string[] HardCodedProgramFilesPaths =
	[
		@"C:\Program Files\Microsoft Games\Zoo Tycoon",
		@"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon",
	];

	private static readonly string[] RegistryValueNames =
	[
		"InstallPath", "InstallDir", "InstallLocation", "Path", "GameDir", "GamePath", "Install_Dir", "Install_Path",
	];

	private readonly IFileSystem _fileSystem;
	private readonly IRegistryReader _registry;

	/// <summary>Initialises a new instance.</summary>
	/// <param name="fileSystem">The file-system abstraction.</param>
	/// <param name="registry">The registry abstraction.</param>
	public InstallationLocator(IFileSystem fileSystem, IRegistryReader registry)
	{
		_fileSystem = fileSystem;
		_registry = registry;
	}

	/// <inheritdoc />
	public Task<LocatedDirectory> LocateAsync(string? persistedLastKnownPath, CancellationToken cancellationToken)
	{
		List<LocationProbeAttempt> trail = new();

		if (TryProbe(source: "Persisted last-known", candidate: persistedLastKnownPath, trail, out string? hit))
		{
			return Task.FromResult(new LocatedDirectory(hit, trail));
		}

		foreach (string candidate in HardCodedProgramFilesPaths)
		{
			if (TryProbe(source: candidate, candidate, trail, out hit))
			{
				return Task.FromResult(new LocatedDirectory(hit, trail));
			}
		}

		foreach (string valueName in RegistryValueNames)
		{
			string? raw = _registry.ReadLocalMachineString(ZooKeyPath, valueName);

			if (TryProbe(source: $"HKLM\\{ZooKeyPath}\\{valueName}", candidate: raw, trail, out hit))
			{
				return Task.FromResult(new LocatedDirectory(hit, trail));
			}
		}

		return Task.FromResult(new LocatedDirectory(Path: null, trail));
	}

	private bool TryProbe(string source, string? candidate, List<LocationProbeAttempt> trail, out string? hit)
	{
		if (string.IsNullOrWhiteSpace(candidate))
		{
			trail.Add(new LocationProbeAttempt(source, CandidatePath: null, Failure: LocationProbeFailure.NoValue));
			hit = null;
			return false;
		}

		string normalised;

		try
		{
			normalised = _fileSystem.Path.GetFullPath(candidate);
		}
		catch (Exception)
		{
			trail.Add(new LocationProbeAttempt(source, candidate, LocationProbeFailure.DirectoryMissing));
			hit = null;
			return false;
		}

		if (!_fileSystem.Directory.Exists(normalised))
		{
			trail.Add(new LocationProbeAttempt(source, normalised, LocationProbeFailure.DirectoryMissing));
			hit = null;
			return false;
		}

		if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(normalised, ExeFileName)))
		{
			trail.Add(new LocationProbeAttempt(source, normalised, LocationProbeFailure.NoExe));
			hit = null;
			return false;
		}

		trail.Add(new LocationProbeAttempt(source, normalised, Failure: null));
		hit = normalised;
		return true;
	}
}
