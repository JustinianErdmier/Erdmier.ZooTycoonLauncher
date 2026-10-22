namespace Erdmier.ZooTycoonLauncher.Application.Settings.Save;

/// <summary>FluentValidation rules for <see cref="SaveLauncherOptionsCommand" />.</summary>
public sealed class SaveLauncherOptionsValidator : AbstractValidator<SaveLauncherOptionsCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public SaveLauncherOptionsValidator()
        => RuleFor(command => command.StartupPreference)
           .NotNull()
           .WithMessage(errorMessage: "Choose what the launcher opens at start-up.");
}
