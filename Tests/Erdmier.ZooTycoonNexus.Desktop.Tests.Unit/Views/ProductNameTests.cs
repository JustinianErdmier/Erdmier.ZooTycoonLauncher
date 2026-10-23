using Avalonia.Headless.XUnit;

using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Dialogs;
using Erdmier.ZooTycoonNexus.Desktop.Views;
using Erdmier.ZooTycoonNexus.Desktop.Views.Dialogs;

namespace Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Views;

/// <summary>Verifies that the window titles name the product as Zoo Tycoon Nexus.</summary>
public sealed class ProductNameTests
{
    [ AvaloniaFact ]
    public void MainWindow_Title_NamesZooTycoonNexus()
    {
        MainWindow window = new();

        window.Title.ShouldBe("  Zoo Tycoon Nexus  V1.0.0");
    }

    [ AvaloniaFact ]
    public void SaveChangesPrompt_Title_NamesZooTycoonNexus()
    {
        SaveChangesPromptView view = new() { DataContext = new SaveChangesPromptViewModel() };

        view.Title.ShouldBe("  Zoo Tycoon Nexus");
    }
}
