namespace Erdmier.ZooTycoonNexus.Application.Tests.Unit.IniConfig;

public sealed class RestoreIniSnapshotValidatorTests
{
    private readonly RestoreIniSnapshotValidator _validator = new();

    [ Fact ]
    public void Validate_AcceptsBothIds() => _validator.Validate(new RestoreIniSnapshotCommand(Guid.CreateVersion7(), Guid.CreateVersion7())).IsValid.ShouldBeTrue();

    [ Fact ]
    public void EmptySnapshotId_Fails()
    {
        ValidationResult result = _validator.Validate(new RestoreIniSnapshotCommand(Guid.CreateVersion7(), Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(expected: 1);
        result.Errors[0].ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    [ Fact ]
    public void EmptyInstallationId_Fails()
    {
        ValidationResult result = _validator.Validate(new RestoreIniSnapshotCommand(Guid.Empty, Guid.CreateVersion7()));

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(expected: 1);
        result.Errors[0].ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }
}
