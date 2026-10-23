using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using Erdmier.ZooTycoonLauncher.Desktop.Views;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Views;

/// <summary>
///     Drives the real View → Config layout radio items in <see cref="MainWindow" /> with headless pointer clicks: opening View and Config layout, then clicking INI style, so the
///     menu's own interaction handler toggles the radio (a radio item writes its own check) before the save fails.
/// </summary>
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

        Click(window, items.Single(item => item.Header as string == "View"));
        Click(window, items.Single(item => item.Header as string == "Config layout"));
        Click(window, ini);

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

    // Clicks the centre of a menu item through the top-level it currently lives in: the window for the menu bar, a popup root for submenu items.
    private static void Click(Window window, MenuItem item)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(item) ?? window;
        Point    point    = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), topLevel)!.Value;

        topLevel.MouseMove(point);
        topLevel.MouseDown(point, MouseButton.Left);
        topLevel.MouseUp(point, MouseButton.Left);

        Dispatcher.UIThread.RunJobs();
    }
}
