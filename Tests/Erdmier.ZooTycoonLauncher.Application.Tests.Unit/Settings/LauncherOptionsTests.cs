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
    public async Task Get_ReturnsTheEditorStyle()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(new LauncherSettings { IniEditorStyle = IniEditorStyle.Ini });

        ErrorOr<LauncherOptions> result = await new GetLauncherOptionsHandler(_repository).Handle(new GetLauncherOptionsQuery(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.EditorStyle.ShouldBe(IniEditorStyle.Ini);
    }

    [ Fact ]
    public async Task Save_StoresTheEditorStyle()
    {
        LauncherSettings stored = new();

        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(stored);

        ErrorOr<Success> result = await new SaveLauncherOptionsHandler(_repository).Handle(
            new SaveLauncherOptionsCommand(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, EditorStyle: IniEditorStyle.Ini),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        stored.IniEditorStyle.ShouldBe(IniEditorStyle.Ini);
        await _repository.Received(requiredNumberOfCalls: 1).UpdateAsync(stored, Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_WritesBothValues_AndKeepsTheDefaultInstallation()
    {
        Guid             defaultId = Guid.CreateVersion7();
        LauncherSettings stored    = new() { DefaultInstallationId = defaultId };

        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(stored);

        ErrorOr<Success> result = await new SaveLauncherOptionsHandler(_repository).Handle(
            new SaveLauncherOptionsCommand(LauncherStartupPreference.LastPlayedInstallation, CloseAfterGameLaunch: true, EditorStyle: IniEditorStyle.Friendly),
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
            new SaveLauncherOptionsCommand(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, EditorStyle: IniEditorStyle.Friendly),
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        await _repository.Received(requiredNumberOfCalls: 1).UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>());
    }

    [ Fact ]
    public async Task Save_UpdateThrows_RestoresTheEntityAndRethrows()
    {
        LauncherSettings stored = new()
        {
            LauncherStartupPreference = LauncherStartupPreference.DefaultInstallation,
            CloseAfterGameLaunch      = false
        };

        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(stored);
        _repository.UpdateAsync(Arg.Any<LauncherSettings>(), Arg.Any<CancellationToken>())
                   .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        SaveLauncherOptionsHandler handler = new(_repository);

        await Should.ThrowAsync<InvalidOperationException>(async () => await handler.Handle(
                                                               new SaveLauncherOptionsCommand(LauncherStartupPreference.NoInstallation, CloseAfterGameLaunch: true, EditorStyle: IniEditorStyle.Friendly),
                                                               CancellationToken.None));

        stored.LauncherStartupPreference.ShouldBe(LauncherStartupPreference.DefaultInstallation);
        stored.CloseAfterGameLaunch.ShouldBeFalse();
    }

    [ Fact ]
    public void Validator_NullPreference_Fails()
        => new SaveLauncherOptionsValidator().Validate(new SaveLauncherOptionsCommand(StartupPreference: null!, CloseAfterGameLaunch: false, EditorStyle: IniEditorStyle.Friendly))
                                             .Errors.ShouldContain(error => error.ErrorMessage == "Choose what the launcher opens at start-up.");
}
