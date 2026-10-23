namespace Erdmier.ZooTycoonNexus.Application.Tests.Unit.IniConfig;

public sealed class LocateIniValidatorTests
{
    private readonly LocateIniValidator _validator = new();

    [ Fact ]
    public void Validate_RootedPath_Passes() => _validator.Validate(new LocateIniCommand(Guid.CreateVersion7(), @"D:\Backup\zoo.ini")).IsValid.ShouldBeTrue();

    [ Fact ]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new LocateIniCommand(Guid.Empty, @"D:\zoo.ini")).Errors.ShouldContain(error => error.ErrorMessage == "An installation must be specified.");

    [ Theory ]
    [ InlineData("") ]
    [ InlineData("   ") ]
    [ InlineData(@"zoo.ini") ]
    [ InlineData(@"..\zoo.ini") ]
    [ InlineData(@"C:zoo.ini") ]
    public void Validate_EmptyOrRelativePath_Fails(string path)
        => _validator.Validate(new LocateIniCommand(Guid.CreateVersion7(), path)).Errors.ShouldContain(error => error.ErrorMessage == "Choose a file to copy.");
}
