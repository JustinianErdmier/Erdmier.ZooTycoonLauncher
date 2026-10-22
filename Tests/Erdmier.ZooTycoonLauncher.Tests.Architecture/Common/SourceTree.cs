namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>Enumerates source files under a root for the architecture tests that scan text.</summary>
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
}
