namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class RelocateInstallationValidatorTests
{
    [Fact]
    public async Task ExcludesSelfFromPathUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByPathAsync(@"C:\Games\Self", id, Arg.Any<CancellationToken>()).Returns(false);

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(id, @"C:\Games\Self"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task RejectsBlankPath()
    {
        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(Guid.CreateVersion7(), "   "));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "NewPath");
    }

    [Fact]
    public async Task RejectsDuplicatePath()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByPathAsync(@"C:\Games\Other", id, Arg.Any<CancellationToken>()).Returns(true);

        RelocateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new RelocateInstallationCommand(id, @"C:\Games\Other"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(f => f.PropertyName == "NewPath");
    }
}
