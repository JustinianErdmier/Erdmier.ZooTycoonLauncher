namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Installations;

public sealed class UpdateInstallationValidatorTests
{
    [ Fact ]
    public async Task ExcludesSelfFromNameUniquenessCheck()
    {
        Guid id = Guid.CreateVersion7();

        IInstallationRepository installations = Substitute.For<IInstallationRepository>();

        installations.ExistsByNameAsync(name: "Main", id, Arg.Any<CancellationToken>())
                     .Returns(returnThis: false);

        UpdateInstallationValidator validator = new(installations);

        ValidationResult result = await validator.ValidateAsync(new UpdateInstallationCommand(id, Name: "Main", MakeDefault: false));

        result.IsValid.ShouldBeTrue();
    }
}
