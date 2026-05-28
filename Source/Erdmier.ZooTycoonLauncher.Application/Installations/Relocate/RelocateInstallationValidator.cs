namespace Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;

/// <summary>FluentValidation rules for <see cref="RelocateInstallationCommand" />.</summary>
public sealed class RelocateInstallationValidator : AbstractValidator<RelocateInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Repository used for the uniqueness checks.</param>
    public RelocateInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.NewPath)
            .NotEmpty()
            .WithMessage("Path is required.")
            .MustAsync((command, path, cancellationToken) => PathIsUniqueAsync(command, path, cancellationToken))
            .WithMessage("Another installation already uses this folder.");
    }

    private async Task<bool> PathIsUniqueAsync(RelocateInstallationCommand command, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return !await _installations.ExistsByPathAsync(path, excludeId: command.InstallationId, cancellationToken);
    }
}
