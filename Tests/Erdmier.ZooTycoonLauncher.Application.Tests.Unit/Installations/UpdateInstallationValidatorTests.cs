namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class UpdateInstallationValidatorTests
{
    [Fact]
    public async Task ExcludesSelfFromNameUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();
        installations.ExistsByNameAsync("Main", id, Arg.Any<CancellationToken>()).Returns(false);

        UpdateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new UpdateInstallationCommand(id, "Main", MakeDefault: false));

        result.IsValid.ShouldBeTrue();
    }
}
