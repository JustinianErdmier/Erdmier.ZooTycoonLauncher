namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.IniConfig;

public sealed class SaveIniValidatorTests
{
    private readonly SaveIniValidator _validator = new();

    [ Theory ]
    [ InlineData("user", "screenwidth", "1024") ]
    [ InlineData("UI", "menuMusic", "sounds/caf\u00E9.wav") ]
    [ InlineData("UI", "menuMusic", "") ]
    [ InlineData("user", "fullscreen", "0") ]
    public void Validate_AcceptsValidEdits(string section, string key, string value) => Validate(section, key, value).IsValid.ShouldBeTrue();

    [ Theory ]
    [ InlineData("scenario", "ag", "0") ]
    [ InlineData("user", "lastfile", @"C:\a.zoo") ]
    [ InlineData("user", "UpdateRate", "0") ]
    [ InlineData("user", "UpdateRate", "fast") ]
    [ InlineData("user", "screenwidth", "") ]
    [ InlineData("UI", "menuMusic", "a\r\nb") ]
    [ InlineData("UI", "menuMusic", "\u20AC.wav") ]
    public void Validate_RejectsInvalidEdits(string section, string key, string value)
    {
        ValidationResult result = Validate(section, key, value);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldAllBe(error => !string.IsNullOrWhiteSpace(error.ErrorMessage));
    }

    [ Fact ]
    public void Validate_RejectsAnEmptyIdAndNoEdits()
    {
        ValidationResult result = _validator.Validate(new SaveIniCommand(Guid.Empty, new Dictionary<IniKeyId, string>()));

        result.Errors.Count.ShouldBe(expected: 2);
    }

    private ValidationResult Validate(string section, string key, string value)
        => _validator.Validate(new SaveIniCommand(Guid.CreateVersion7(), new Dictionary<IniKeyId, string> { [new IniKeyId(section, key)] = value }));
}
