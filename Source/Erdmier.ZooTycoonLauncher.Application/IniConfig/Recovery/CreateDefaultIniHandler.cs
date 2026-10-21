namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>Handler for <see cref="CreateDefaultIniCommand" />.</summary>
public sealed class CreateDefaultIniHandler : ICommandHandler<CreateDefaultIniCommand, ErrorOr<IniRecoveryResult>>
{
    private readonly IInstallationRepository _installations;

    private readonly IniRecoverySteps _steps;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Installation repository.</param>
    /// <param name="steps">The shared create-and-import steps.</param>
    public CreateDefaultIniHandler(IInstallationRepository installations, IniRecoverySteps steps)
    {
        _installations = installations;
        _steps         = steps;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniRecoveryResult>> Handle(CreateDefaultIniCommand command, CancellationToken cancellationToken)
    {
        GameInstallation? row = await _installations.GetByIdAsync(command.InstallationId, cancellationToken);

        if (row is null)
        {
            return IniErrors.InstallationNotFound(command.InstallationId);
        }

        return await _steps.CreateAndImportAsync(row, ZooIniDefaultDocument.Render(), cancellationToken);
    }
}
