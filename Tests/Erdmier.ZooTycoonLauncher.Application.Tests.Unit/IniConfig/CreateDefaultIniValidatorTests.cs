namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class CreateDefaultIniValidatorTests
{
    private readonly CreateDefaultIniValidator _validator = new();

    [ Fact ]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new CreateDefaultIniCommand(Guid.Empty)).Errors.ShouldContain(error => error.ErrorMessage == "An installation must be specified.");

    [ Fact ]
    public void Validate_RealId_Passes() => _validator.Validate(new CreateDefaultIniCommand(Guid.CreateVersion7())).IsValid.ShouldBeTrue();
}
