namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>Handler for <see cref="LocateIniCommand" />.</summary>
public sealed class LocateIniHandler : ICommandHandler<LocateIniCommand, ErrorOr<IniRecoveryResult>>
{
    private const long MaxSourceBytes = 1_048_576;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly IniRecoverySteps _steps;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="files">The INI file store, used to read the chosen source.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="steps">The shared create-and-import steps.</param>
    public LocateIniHandler(IIniFileStore files, IInstallationRepository installations, IniRecoverySteps steps)
    {
        _files         = files;
        _installations = installations;
        _steps         = steps;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniRecoveryResult>> Handle(LocateIniCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return IniErrors.InstallationNotFound(command.InstallationId);
        }

        string? text;

        try
        {
            text = await _files.ReadExternalAsync(command.SourcePath, MaxSourceBytes, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return IniErrors.ReadFailed(ex.Message);
        }

        if (text is null || !HasRecognisedSection(text))
        {
            return IniErrors.NotAnIni(command.SourcePath);
        }

        return await _steps.CreateAndImportAsync(row, text, cancellationToken);
    }

    private static bool HasRecognisedSection(string text)
    {
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.Length < 3 || line[0] != '[' || line[^1] != ']')
            {
                continue;
            }

            string name = line[1..^1]
               .Trim();

            if (ZooIniDefaults.Sections.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
