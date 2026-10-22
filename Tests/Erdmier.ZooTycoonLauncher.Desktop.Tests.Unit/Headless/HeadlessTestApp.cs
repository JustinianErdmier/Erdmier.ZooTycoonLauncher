using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Headless;

/// <summary>
///     The Avalonia application the headless tests run under. It carries the real <see cref="App" />'s resources, styles, data templates and theme variant, but none of its
///     start-up: <see cref="App.OnFrameworkInitializationCompleted" /> composes the service provider and opens the launcher's databases, which a view-load test must not do.
/// </summary>
public sealed class HeadlessTestApp : Avalonia.Application
{
    /// <inheritdoc />
    /// <remarks>
    ///     Loads <c>App.axaml</c> into a throwaway <see cref="App" /> and moves what it built across, so the tests always see exactly what the launcher ships rather than a
    ///     hand-kept copy of it. Each piece is detached from the donor before it is attached here, because a resource dictionary or style accepts only one owner.
    /// </remarks>
    public override void Initialize()
    {
        App donor = new();

        donor.Initialize();

        RequestedThemeVariant = donor.RequestedThemeVariant;

        IResourceDictionary resources = donor.Resources;

        donor.Resources = new ResourceDictionary();
        Resources       = resources;

        foreach (IStyle style in donor.Styles.ToList())
        {
            donor.Styles.Remove(style);
            Styles.Add(style);
        }

        foreach (IDataTemplate template in donor.DataTemplates.ToList())
        {
            donor.DataTemplates.Remove(template);
            DataTemplates.Add(template);
        }
    }
}
