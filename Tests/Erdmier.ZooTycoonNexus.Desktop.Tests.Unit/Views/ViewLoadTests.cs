using System.Reflection;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using Erdmier.ZooTycoonNexus.Desktop.Views;

namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Views;

/// <summary>
///     Loads every view in the Desktop project under the real application resources, so a failure that only shows at run time — a missing <c>StaticResource</c> key, an
///     inaccessible <c>AvaloniaProperty</c> field, a bad markup-extension value — fails a test instead of crashing the launcher.
/// </summary>
public sealed class ViewLoadTests
{
    private const string ViewsNamespace = "Erdmier.ZooTycoonNexus.Desktop.Views";

    private const string ViewModelsNamespace = "Erdmier.ZooTycoonNexus.Desktop.ViewModels";

    /// <summary>The full name of every view: each <see cref="UserControl" /> or <see cref="Window" /> under <c>Views/</c>, which includes <see cref="MainWindow" />.</summary>
    public static TheoryData<string> ViewTypeNames
    {
        get
        {
            TheoryData<string> data = [];

            foreach (Type type in EnumerateViewTypes())
            {
                data.Add(type.FullName!);
            }

            return data;
        }
    }

    /// <summary>The view enumeration finds every view, so a broken filter cannot pass the theory vacuously.</summary>
    [ Fact ]
    public void Enumeration_FindsEveryView()
    {
        List<Type> views = EnumerateViewTypes()
            .ToList();

        views.ShouldContain(typeof(MainWindow));
        views.ShouldContain(typeof(Desktop.Views.Boot.PlayView));
        views.ShouldContain(typeof(Desktop.Views.Dialogs.OptionsDialogView));
        views.ShouldContain(typeof(Desktop.Views.IniConfig.Fields.IniNumberFieldView));
    }

    /// <summary>
    ///     The view is created, given its view model from the designer constructor, shown and laid out without throwing, and without Avalonia logging a warning or error — a
    ///     binding that cannot convert its value, say, which Avalonia reports and recovers from rather than throws.
    /// </summary>
    /// <param name="viewTypeName">The full name of the view type under test.</param>
    [ AvaloniaTheory ]
    [ MemberData(nameof(ViewTypeNames)) ]
    public void View_LoadsWithItsDesignerViewModel(string viewTypeName)
    {
        HeadlessTestAppBuilder.LogSink.Clear();

        Type viewType = typeof(App).Assembly.GetType(viewTypeName, throwOnError: true)!;

        object viewModel = viewType == typeof(MainWindow) ? CreateMainWindowViewModel() : CreateDesignerViewModel(viewType);

        Control view = (Control)Activator.CreateInstance(viewType)!;

        Window window = view as Window ?? new Window { Content = view };

        view.DataContext = viewModel;

        window.Show();

        Dispatcher.UIThread.RunJobs();

        view.IsMeasureValid.ShouldBeTrue($"{viewType.Name} was not laid out.");

        window.Close();

        Dispatcher.UIThread.RunJobs();

        IReadOnlyList<string> logged = HeadlessTestAppBuilder.LogSink.Messages;

        logged.ShouldBeEmpty($"{viewType.Name} made Avalonia log a warning or error:" + Environment.NewLine + string.Join(Environment.NewLine, logged));
    }

    private static IEnumerable<Type> EnumerateViewTypes()
        => typeof(App).Assembly.GetTypes()
                      .Where(type => type is { IsAbstract: false, IsPublic: true, Namespace: not null }
                                     && type.Namespace.StartsWith(ViewsNamespace, StringComparison.Ordinal)
                                     && (typeof(UserControl).IsAssignableFrom(type) || typeof(Window).IsAssignableFrom(type)))
                      .OrderBy(type => type.FullName, StringComparer.Ordinal);

    /// <summary>
    ///     Builds the view model for <paramref name="viewType" /> the way the view-model pair rule names it — <c>Views.X.FooView</c> pairs with <c>ViewModels.X.FooViewModel</c> —
    ///     through its parameterless designer constructor.
    /// </summary>
    private static object CreateDesignerViewModel(Type viewType)
    {
        string viewModelNamespace = ViewModelsNamespace + viewType.Namespace![ ViewsNamespace.Length.. ];
        string viewModelName      = viewType.Name + "Model";

        Type? viewModelType = typeof(App).Assembly.GetType($"{viewModelNamespace}.{viewModelName}");

        viewModelType.ShouldNotBeNull($"{viewType.Name} has no view model named {viewModelNamespace}.{viewModelName}.");

        ConstructorInfo? designerConstructor = viewModelType.GetConstructor(Type.EmptyTypes);

        designerConstructor.ShouldNotBeNull($"{viewModelType.Name} has no parameterless designer constructor.");

        return designerConstructor.Invoke(parameters: null);
    }

    /// <summary>
    ///     <see cref="MainWindowViewModel" /> is the pair rule's documented exception and has no designer constructor, so it is built from substitutes. <see cref="MainWindow" />
    ///     boots on load; the stubbed boot lands on No Game Installation Found, so the window also resolves a state view through the <see cref="ViewLocator" />.
    /// </summary>
    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        IMediator mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<BootCommand>(), Arg.Any<CancellationToken>())
                .Returns(new ValueTask<ErrorOr<BootResult>>(new BootResult(BootOutcome.NoGameInstallationFound, ActiveInstallation: null, LocatedCandidatePath: null)));

        return new MainWindowViewModel(mediator,
                                       Substitute.For<IApplicationLifecycle>(),
                                       Substitute.For<IDialogService>(),
                                       new WeakReferenceMessenger(),
                                       NullLogger<MainWindowViewModel>.Instance,
                                       NullLogger<InstallationGridViewModel>.Instance,
                                       NullLogger<PlayViewModel>.Instance,
                                       NullLogger<IniConfigTabViewModel>.Instance,
                                       NullLogger<IniEditorViewModel>.Instance,
                                       NullLogger<IniPlaceholderViewModel>.Instance,
                                       new IniEditorStyleState());
    }
}
