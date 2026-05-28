namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class AddInstallationValidatorTests
{
    [ Fact ]
    public async Task RejectsBlankName()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "   ", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "Name");
    }

    [ Fact ]
    public async Task RejectsDuplicateName()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.ExistsByNameAsync(name: "Main", excludeId: null, Arg.Any<CancellationToken>())
                     .Returns(returnThis: true);

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "Name");
    }

    [ Fact ]
    public async Task AcceptsUniqueRow()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        AddInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new AddInstallationCommand(Name: "Main", Path: @"C:\Games\Main", MakeDefault: false));

        result.IsValid.ShouldBeTrue();
    }
}
