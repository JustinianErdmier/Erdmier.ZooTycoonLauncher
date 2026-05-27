namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class NoFilesAtAssemblyRootTests
{
    public static IEnumerable<object[]> SourceProjectDirectories()
    {
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Domain")];
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Application")];
        yield return [ResolveProjectDirectory("Erdmier.ZooTycoonLauncher.Infrastructure")];
    }

    [Theory]
    [MemberData(nameof(SourceProjectDirectories))]
    public void NoCsFileSitsAtProjectRoot(string projectDirectory)
    {
        string[] rootCsFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(p => !Path.GetFileName(p).Equals("GlobalUsings.cs", StringComparison.Ordinal))
            .ToArray();

        rootCsFiles.ShouldBeEmpty(
            $"Files at project root in {Path.GetFileName(projectDirectory)}:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", rootCsFiles.Select(Path.GetFileName))}");
    }

    private static string ResolveProjectDirectory(string assemblyName)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "Source", assemblyName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/{assemblyName} from {AppContext.BaseDirectory}.");
    }
}
