namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Settings;

public sealed class LauncherOptionsTests
{
    private readonly ILauncherSettingsRepository _repository = Substitute.For<ILauncherSettingsRepository>();

    [ Fact ]
    public async Task Get_ReturnsTheStoredValues()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>())
                   .Returns(new LauncherSettings
                   {
                       CloseAfterGameLaunch      = true,
                       LauncherStartupPreference = LauncherStartupPreference.NoInstallation
                   });

        ErrorOr<LauncherOptions> result = await new GetLauncherOptionsHandler(_repository).Handle(new GetLauncherOptionsQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.StartupPreference.ShouldBe(LauncherStartupPreference.NoInstallation);
        result.Value.CloseAfterGameLaunch.ShouldBeTrue();
    }

    [ Fact ]
    public async Task Save_WritesBothValues_AndKeepsTheDefaultInstallation()
    {
        Guid             defaultId = Guid.CreateVersion7();
        LauncherSettings stored    = new() { DefaultInstallationId = defaultId };
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(stored);

        ErrorOr<Success> result = await new SaveLauncherOptionsHandler(_repository).Handle(
            new SaveLauncherOptionsCommand(LauncherStartupPreference.LastPlayedInstallation, CloseAfterGameLaunch: true),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        await _repository.Received(requiredNumberOfCalls: 1).UpdateAsync(
            Arg.Is<LauncherSettings>(settings => settings.LauncherStartupPreference == LauncherStartupPreference.LastPlayedInstallation
                                                 && settings.CloseAfterGameLaunch
                                                 && settings.DefaultInstallationId == defaultId),
            Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_UnchangedValues_StillSaves()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings());

        ErrorOr<Success> result = await new SaveLauncherOptionsHandler(_repository).Handle(
            new SaveLauncherOptionsCommand(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        await _repository.Received(requiredNumberOfCalls: 1).UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public void Validator_NullPreference_Fails()
        => new SaveLauncherOptionsValidator().Validate(new SaveLauncherOptionsCommand(StartupPreference: null!, CloseAfterGameLaunch: false))
                                             .Errors.ShouldContain(error => error.ErrorMessage == "Choose what the launcher opens at start-up.");
}
