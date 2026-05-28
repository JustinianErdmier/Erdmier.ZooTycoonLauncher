namespace Erdmier.ZooTycoonLauncher.Application.Installations.Add;

/// <summary>FluentValidation rules for <see cref="AddInstallationCommand" />.</summary>
public sealed class AddInstallationValidator : AbstractValidator<AddInstallationCommand>
{
    private readonly IInstallationRepository _installations;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="installations">Repository used for the uniqueness checks.</param>
    public AddInstallationValidator(IInstallationRepository installations)
    {
        _installations = installations;

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage(errorMessage: "Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
            .WithMessage(errorMessage: "Name cannot be whitespace.")
            .MustAsync(NameIsUniqueAsync)
            .WithMessage(errorMessage: "Another installation already uses this name.");

        RuleFor(c => c.Path)
            .NotEmpty()
            .WithMessage(errorMessage: "Path is required.")
            .MustAsync(PathIsUniqueAsync)
            .WithMessage(errorMessage: "Another installation already uses this folder.");
    }

    private async Task<bool> NameIsUniqueAsync(string name, CancellationToken cancellationToken)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return true;
        }

        return !await _installations.ExistsByNameAsync(trimmed, excludeId: null, cancellationToken);
    }

    private async Task<bool> PathIsUniqueAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        return !await _installations.ExistsByPathAsync(path, excludeId: null, cancellationToken);
    }
}
