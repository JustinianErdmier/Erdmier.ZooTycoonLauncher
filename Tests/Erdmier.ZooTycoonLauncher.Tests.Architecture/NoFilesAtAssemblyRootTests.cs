namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class NoFilesAtAssemblyRootTests
{
    [ Theory ]
    [ MemberData(nameof(SourceProjectDirectories)) ]
    public void NoCsFileSitsAtProjectRoot(string projectDirectory)
    {
        // GlobalUsings.cs and Avalonia entry-point files (App.axaml.cs, Program.cs) are
        // conventional root-level files exempt from the no-root-files rule.
        string[] exemptFileNames = ["GlobalUsings.cs", "App.axaml.cs", "Program.cs"];

        string[] rootCsFiles =
        [
            .. Directory.GetFiles(projectDirectory, searchPattern: "*.cs", SearchOption.TopDirectoryOnly)
                        .Where(p => !exemptFileNames.Contains(Path.GetFileName(p), StringComparer.Ordinal))
        ];

        rootCsFiles.ShouldBeEmpty($"Files at project root in {Path.GetFileName(projectDirectory)}:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ",
                                                                                                                                        rootCsFiles.Select(Path.GetFileName))}");
    }

    public static IEnumerable<object[]> SourceProjectDirectories()
    {
        yield return [ProjectPaths.Domain];
        yield return [ProjectPaths.Application];
        yield return [ProjectPaths.Infrastructure];
        yield return [ProjectPaths.Desktop];
    }
}
