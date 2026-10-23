using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using Erdmier.ZooTycoonLauncher.Desktop.Views;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views;

/// <summary>Drives the real View → Config layout radio items in <see cref="MainWindow" />, which write their own check on click.</summary>
public sealed class MainWindowStyleMenuTests
{
    [ AvaloniaFact ]
    public async Task ClickingIniStyle_WhenTheSaveFails_SnapsTheBulletBackToFriendly()
    {
        IDialogService dialogs  = Substitute.For<IDialogService>();
        IMediator      mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ErrorOr<BootResult>>(new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null)));

        mediator.Send(Arg.Any<GetLauncherOptionsQuery>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ErrorOr<LauncherOptions>>(new LauncherOptions(LauncherStartupPreference.DefaultInstallation, CloseAfterGameLaunch: false, IniEditorStyle.Friendly)));

        mediator.Send(Arg.Any<SaveLauncherOptionsCommand>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ErrorOr<Success>>(Error.Failure(code: "Settings.WriteFailed", description: "disk full")));

        IniEditorStyleState styleState = new();

        MainWindowViewModel viewModel = new(mediator,
                                            Substitute.For<IApplicationLifecycle>(),
                                            dialogs,
                                            new WeakReferenceMessenger(),
                                            NullLogger<MainWindowViewModel>.Instance,
                                            NullLogger<InstallationGridViewModel>.Instance,
                                            NullLogger<PlayViewModel>.Instance,
                                            NullLogger<IniConfigTabViewModel>.Instance,
                                            NullLogger<IniEditorViewModel>.Instance,
                                            NullLogger<IniPlaceholderViewModel>.Instance,
                                            styleState);

        MainWindow window = new() { DataContext = viewModel };

        window.Show();

        Dispatcher.UIThread.RunJobs();

        List<MenuItem> items    = window.GetLogicalDescendants().OfType<MenuItem>().ToList();
        MenuItem       friendly = items.Single(item => item.Header as string == "Friendly style");
        MenuItem       ini      = items.Single(item => item.Header as string == "INI style");

        friendly.IsChecked.ShouldBeTrue();
        ini.IsChecked.ShouldBeFalse();

        ini.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        for (int attempt = 0; attempt < 100 && !dialogs.ReceivedCalls().Any(); attempt++)
        {
            await Task.Delay(millisecondsDelay: 10);

            Dispatcher.UIThread.RunJobs();
        }

        await dialogs.Received(requiredNumberOfCalls: 1).ShowErrorAsync("Cannot Save Options", "disk full");

        Dispatcher.UIThread.RunJobs();

        styleState.Style.ShouldBe(IniEditorStyle.Friendly);
        friendly.IsChecked.ShouldBeTrue();
        ini.IsChecked.ShouldBeFalse();

        window.Close();
    }
}
