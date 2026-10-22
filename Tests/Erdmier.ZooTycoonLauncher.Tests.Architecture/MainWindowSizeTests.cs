namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class MainWindowSizeTests
{
    /// <summary>SDD §11.4 caps MainWindow.axaml at 100 lines to keep it host-only.</summary>
    [ Fact ]
    public void MainWindowAxaml_DoesNotExceed100Lines()
    {
        string projectRoot    = ProjectPaths.Desktop;
        string mainWindowPath = Path.Combine(projectRoot, path2: "Views", path3: "MainWindow.axaml");

        File.Exists(mainWindowPath)
            .ShouldBeTrue($"Expected MainWindow.axaml at {mainWindowPath}.");

        int lineCount = File.ReadAllLines(mainWindowPath)
                            .Length;

        lineCount.ShouldBeLessThanOrEqualTo(expected: 100, $"MainWindow.axaml has {lineCount} lines — cap is 100 (SDD §11.4).");
    }
}
