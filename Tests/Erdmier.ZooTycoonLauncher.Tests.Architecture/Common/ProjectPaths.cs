namespace Erdmier.ZooTycoonLauncher.Tests.Architecture.Common;

/// <summary>Locates the production project directories under <c>Source/</c> for the architecture tests that scan files.</summary>
public static class ProjectPaths
{
    private const string Prefix = "Erdmier.ZooTycoonLauncher.";

    private static readonly Lazy<string> SourceDirectory = new(LocateSource);

    /// <summary>Gets the absolute path of the repository's <c>Source</c> directory.</summary>
    public static string Source => SourceDirectory.Value;

    /// <summary>Gets the absolute path of the UI conventions document, <c>Docs/UserInterfaceDesign/conventions.md</c>.</summary>
    public static string Conventions => Path.Combine(Directory.GetParent(Source)!.FullName, "Docs", "UserInterfaceDesign", "conventions.md");

    /// <summary>Gets the absolute path of the Domain project directory.</summary>
    public static string Domain => Project(Prefix + "Domain");

    /// <summary>Gets the absolute path of the Application project directory.</summary>
    public static string Application => Project(Prefix + "Application");

    /// <summary>Gets the absolute path of the Infrastructure project directory.</summary>
    public static string Infrastructure => Project(Prefix + "Infrastructure");

    /// <summary>Gets the absolute path of the Desktop project directory.</summary>
    public static string Desktop => Project(Prefix + "Desktop");

    /// <summary>Gets the absolute path of a project directory under <c>Source</c>.</summary>
    /// <param name="assemblyName">The project folder (and assembly) name, for example <c>Erdmier.ZooTycoonLauncher.Desktop</c>.</param>
    /// <returns>The absolute path of the project directory.</returns>
    /// <exception cref="InvalidOperationException">The project directory, or its project file, does not exist.</exception>
    public static string Project(string assemblyName)
    {
        string candidate = Path.Combine(Source, assemblyName);

        return Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, assemblyName + ".csproj"))
                   ? candidate
                   : throw new InvalidOperationException($"Could not locate Source/{assemblyName}/{assemblyName}.csproj under {Source}.");
    }

    private static string LocateSource()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, path2: "Source");

            if (Directory.Exists(Path.Combine(candidate, Prefix + "Desktop")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate the Source directory from {AppContext.BaseDirectory}.");
    }
}
