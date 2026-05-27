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
        yield return [ResolveProjectDirectory(assemblyName: "Erdmier.ZooTycoonLauncher.Domain")];
        yield return [ResolveProjectDirectory(assemblyName: "Erdmier.ZooTycoonLauncher.Application")];
        yield return [ResolveProjectDirectory(assemblyName: "Erdmier.ZooTycoonLauncher.Infrastructure")];
        yield return [ResolveProjectDirectory(assemblyName: "Erdmier.ZooTycoonLauncher.Desktop")];
    }

    private static string ResolveProjectDirectory(string assemblyName)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, path2: "Source", assemblyName);

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/{assemblyName} from {AppContext.BaseDirectory}.");
    }
}
