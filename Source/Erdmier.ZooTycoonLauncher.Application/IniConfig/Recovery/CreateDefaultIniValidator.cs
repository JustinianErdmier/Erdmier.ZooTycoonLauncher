namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Recovery;

/// <summary>FluentValidation rules for <see cref="CreateDefaultIniCommand" />.</summary>
public sealed class CreateDefaultIniValidator : AbstractValidator<CreateDefaultIniCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public CreateDefaultIniValidator()
        => RuleFor(command => command.InstallationId)
            .NotEmpty()
            .WithMessage(errorMessage: "An installation must be specified.");
}
