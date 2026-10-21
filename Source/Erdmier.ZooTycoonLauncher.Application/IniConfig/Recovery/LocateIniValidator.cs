namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>FluentValidation rules for <see cref="LocateIniCommand" />.</summary>
public sealed class LocateIniValidator : AbstractValidator<LocateIniCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public LocateIniValidator()
    {
        RuleFor(command => command.InstallationId)
            .NotEmpty()
            .WithMessage(errorMessage: "An installation must be specified.");

        RuleFor(command => command.SourcePath)
            .Must(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path))
            .WithMessage(errorMessage: "Choose a file to copy.");
    }
}
