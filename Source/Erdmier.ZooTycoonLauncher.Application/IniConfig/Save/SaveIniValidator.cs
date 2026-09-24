namespace Erdmier.ZooTycoonLauncher.Application.IniConfig.Save;

/// <summary>FluentValidation rules for <see cref="SaveIniCommand" />. Messages are shown verbatim in the editor's error dialogue.</summary>
public sealed class SaveIniValidator : AbstractValidator<SaveIniCommand>
{
    /// <summary>Initialises a new instance.</summary>
    public SaveIniValidator()
    {
        RuleFor(command => command.InstallationId)
            .NotEmpty()
            .WithMessage(errorMessage: "An installation must be specified.");

        RuleFor(command => command.Edits)
            .NotEmpty()
            .WithMessage(errorMessage: "There are no changes to save.");

        RuleForEach(command => command.Edits)
            .Custom((edit, context) =>
            {
                if (!ZooIniDefaults.TryGet(edit.Key, out IniKeySpec? spec))
                {
                    context.AddFailure($"{edit.Key} is not a setting the launcher recognises.");

                    return;
                }

                if (spec.Role != IniKeyRole.UserSetting)
                {
                    context.AddFailure($"{spec.Id} is managed by the game and cannot be edited.");

                    return;
                }

                if (!spec.IsValid(edit.Value))
                {
                    context.AddFailure($"\"{edit.Value}\" is not a valid value for {spec.Id}.");

                    return;
                }

                if (edit.Value.Any(character => character is '\r' or '\n'))
                {
                    context.AddFailure($"{spec.Id} cannot contain a line break.");
                }
                else if (edit.Value.Any(character => character > '\u00FF'))
                {
                    context.AddFailure($"{spec.Id} contains a character zoo.ini cannot store.");
                }
            });
    }
}
