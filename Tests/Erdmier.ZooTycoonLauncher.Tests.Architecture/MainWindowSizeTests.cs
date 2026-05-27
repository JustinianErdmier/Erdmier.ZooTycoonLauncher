namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class MainWindowSizeTests
{
    /// <summary>SDD §11.4 caps MainWindow.axaml at 100 lines to keep it host-only.</summary>
    [Fact]
    public void MainWindowAxaml_DoesNotExceed100Lines()
    {
        string projectRoot = ResolveDesktopProjectDirectory();
        string mainWindowPath = Path.Combine(projectRoot, "Views", "MainWindow.axaml");

        File.Exists(mainWindowPath).ShouldBeTrue($"Expected MainWindow.axaml at {mainWindowPath}.");

        int lineCount = File.ReadAllLines(mainWindowPath).Length;

        lineCount.ShouldBeLessThanOrEqualTo(100, $"MainWindow.axaml has {lineCount} lines — cap is 100 (SDD §11.4).");
    }

    private static string ResolveDesktopProjectDirectory()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "Source", "Erdmier.ZooTycoonLauncher.Desktop");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/Erdmier.ZooTycoonLauncher.Desktop from {AppContext.BaseDirectory}.");
    }
}
