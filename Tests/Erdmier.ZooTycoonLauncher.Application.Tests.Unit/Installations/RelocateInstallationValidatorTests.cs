namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class RelocateInstallationValidatorTests
{
    [ Fact ]
    public async Task ExcludesSelfFromPathUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.ExistsByPathAsync(path: @"C:\Games\Self", id, Arg.Any<CancellationToken>())
                     .Returns(returnThis: false);

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(id, NewPath: @"C:\Games\Self"));

        result.IsValid.ShouldBeTrue();
    }

    [ Fact ]
    public async Task RejectsBlankPath()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(Guid.CreateVersion7(), NewPath: "   "));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "NewPath");
    }

    [ Fact ]
    public async Task RejectsDuplicatePath()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.ExistsByPathAsync(path: @"C:\Games\Other", id, Arg.Any<CancellationToken>())
                     .Returns(returnThis: true);

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(id, NewPath: @"C:\Games\Other"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "NewPath");
    }
}
