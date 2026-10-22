namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>Locates the production projects and enumerates their source files for the architecture tests that scan text.</summary>
public static class SourceTree
{
    /// <summary>Enumerates the files under <paramref name="root" /> matching <paramref name="pattern" />, skipping <c>bin</c> and <c>obj</c>.</summary>
    /// <param name="root">The project directory.</param>
    /// <param name="pattern">The file search pattern, for example <c>*.axaml</c>.</param>
    /// <returns>The matching file paths.</returns>
    public static IEnumerable<string> EnumerateSources(string root, string pattern)
        => Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        string relative = Path.GetRelativePath(root, f);

                        return !relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                               && !relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                    });

    /// <summary>Walks up from the test output directory to find <c>Source/&lt;project&gt;</c>.</summary>
    /// <param name="project">The project folder name.</param>
    /// <returns>The absolute path of the project directory.</returns>
    /// <exception cref="InvalidOperationException">The project directory cannot be found.</exception>
    public static string ResolveProjectDirectory(string project)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, path2: "Source", project);

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate Source/{project} from {AppContext.BaseDirectory}.");
    }
}
