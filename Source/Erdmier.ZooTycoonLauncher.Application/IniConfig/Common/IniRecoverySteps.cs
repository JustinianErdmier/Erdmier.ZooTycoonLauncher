namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

/// <summary>
///     The steps Create and Locate share (milestone 10 spec, §5.1): a create-only write of <c>zoo.ini</c>, then the row update, the change message and the snapshot import.
///     A row-update or snapshot failure after the file exists is logged and swallowed, as in SDD §8.2.
/// </summary>
public sealed class IniRecoverySteps
{
    private readonly TimeProvider _clock;

    private readonly IApplicationEventPublisher _events;

    private readonly IIniFileStore _files;

    private readonly IInstallationRepository _installations;

    private readonly ILogger<IniRecoverySteps> _logger;

    private readonly IInstallationWriteProbe _probe;

    private readonly IIniSnapshotService _snapshots;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="events">Publishes installation-change messages after the row is persisted.</param>
    /// <param name="files">The <c>zoo.ini</c> file store.</param>
    /// <param name="installations">Installation repository.</param>
    /// <param name="logger">Logger for write and snapshot failures.</param>
    /// <param name="probe">Classifies an access-denied write failure.</param>
    /// <param name="snapshots">Imports the new file into the snapshot store.</param>
    /// <param name="clock">Time provider for the <c>ModifiedUtc</c> stamp.</param>
    public IniRecoverySteps(IApplicationEventPublisher events,
                            IIniFileStore              files,
                            IInstallationRepository    installations,
                            ILogger<IniRecoverySteps>  logger,
                            IInstallationWriteProbe    probe,
                            IIniSnapshotService        snapshots,
                            TimeProvider               clock)
    {
        _events        = events;
        _files         = files;
        _installations = installations;
        _logger        = logger;
        _probe         = probe;
        _snapshots     = snapshots;
        _clock         = clock;
    }

    /// <summary>Writes <c>zoo.ini</c> only if it is absent, then records the file in the row and the snapshot store.</summary>
    /// <param name="row">The installation row to update.</param>
    /// <param name="text">The full text to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installation's new validity, <c>Ini.AlreadyExists</c>, <c>Ini.ReadOnly</c> or <c>Ini.WriteFailed</c>.</returns>
    public async Task<ErrorOr<IniRecoveryResult>> CreateAndImportAsync(GameInstallation row, string text, CancellationToken cancellationToken)
    {
        try
        {
            await _files.DeleteOrphanedTempFilesAsync(row.Path, cancellationToken);

            if (!await _files.TryCreateAsync(row.Path, text, cancellationToken))
            {
                return IniErrors.AlreadyExists(row.Path);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, message: "Creating zoo.ini was denied for {InstallationId}", row.Id);

            InstallationWriteAccess access = await _probe.ProbeAsync(row.Path, cancellationToken);

            return IniReadOnlyText.For(access) is null ? IniErrors.WriteFailed(ex.Message) : IniErrors.ReadOnly(access);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, message: "Creating zoo.ini failed for {InstallationId}", row.Id);

            return IniErrors.WriteFailed(ex.Message);
        }

        row.HasIni = true;

        row.ModifiedUtc = _clock.GetUtcNow()
                                .UtcDateTime;

        try
        {
            await _installations.UpdateAsync(row, cancellationToken);

            _events.Publish(new InstallationChangedMessage(row.Id));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "zoo.ini was created for {InstallationId} but its installation row could not be updated", row.Id);
        }

        try
        {
            ErrorOr<Success> synchronised = await _snapshots.SynchroniseAsync(row, cancellationToken);

            if (synchronised.IsError)
            {
                _logger.LogWarning(message: "zoo.ini was created for {InstallationId} but its snapshot could not be recorded: {Error}",
                                   row.Id,
                                   synchronised.FirstError.Description);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, message: "zoo.ini was created for {InstallationId} but its snapshot could not be recorded", row.Id);
        }

        return new IniRecoveryResult(row.Validity);
    }
}
