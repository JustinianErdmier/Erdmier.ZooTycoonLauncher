namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Restore;

/// <summary>FluentValidation rules for <see cref="RestoreIniSnapshotCommand" />.</summary>
public sealed class RestoreIniSnapshotValidator : AbstractValidator<RestoreIniSnapshotCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public RestoreIniSnapshotValidator()
    {
        RuleFor(command => command.InstallationId)
            .NotEmpty()
            .WithMessage(errorMessage: "An installation must be specified.");

        RuleFor(command => command.SnapshotId)
            .NotEmpty()
            .WithMessage(errorMessage: "A version of zoo.ini must be specified.");
    }
}
