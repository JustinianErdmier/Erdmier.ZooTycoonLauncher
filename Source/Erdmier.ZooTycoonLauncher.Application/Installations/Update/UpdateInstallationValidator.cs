namespace Erdmier.ZooTycoonLauncher.Application.Installations.Update;

/// <summary>FluentValidation rules for <see cref="UpdateInstallationCommand" />.</summary>
public sealed class UpdateInstallationValidator : AbstractValidator<UpdateInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    public UpdateInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
            .WithMessage("Name cannot be whitespace.")
            .MustAsync(NameIsUniqueAsync)
            .WithMessage("Another installation already uses this name.");
    }

    private async Task<bool> NameIsUniqueAsync(UpdateInstallationCommand command, string name, CancellationToken cancellationToken)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return true;
        }

        return !await _installations.ExistsByNameAsync(trimmed, excludeId: command.InstallationId, cancellationToken);
    }
}
