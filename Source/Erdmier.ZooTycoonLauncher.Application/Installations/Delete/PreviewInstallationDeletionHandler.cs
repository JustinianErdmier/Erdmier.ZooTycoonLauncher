namespace Erdmier.ZooTycoonLauncher.Application.Installations.Delete;

/// <summary>
///     Handler for <see cref="PreviewInstallationDeletionQuery" />. Uses the same promotion rule as <see cref="DeleteInstallationHandler" /> (excluding the row being
///     previewed), so the confirmation cannot name a different successor from the one the delete promotes.
/// </summary>
public sealed class PreviewInstallationDeletionHandler : IQueryHandler<PreviewInstallationDeletionQuery, ErrorOr<InstallationDeletionPreview>>
{
    private readonly IInstallationRepository _installations;

    private readonly ILauncherSettingsRepository _settings;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="settings">Launcher settings repository.</param>
    public PreviewInstallationDeletionHandler(IInstallationRepository installations, ILauncherSettingsRepository settings)
    {
        _installations = installations;
        _settings      = settings;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<InstallationDeletionPreview>> Handle(PreviewInstallationDeletionQuery query, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(query.InstallationId, cancellationToken);

        if (row is null)
        {
            return Error.NotFound(code: "Installation.NotFound", $"No installation with id {query.InstallationId}.");
        }

        LauncherSettings settings  = await _settings.GetAsync(cancellationToken);
        bool             isDefault = settings.DefaultInstallationId == row.Id;

        if (!isDefault)
        {
            return new InstallationDeletionPreview(row.Name, IsDefault: false, PromotedName: null);
        }

        GameInstallation? successor = await _installations.FindDefaultPromotionCandidateAsync(row.Id, cancellationToken);

        return new InstallationDeletionPreview(row.Name, IsDefault: true, successor?.Name);
    }
}
