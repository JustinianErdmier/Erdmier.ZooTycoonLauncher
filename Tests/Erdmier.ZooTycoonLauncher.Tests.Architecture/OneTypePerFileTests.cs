namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class OneTypePerFileTests
{
    public static IEnumerable<object[]> SourceProjectAssemblies()
    {
        yield return [typeof(Domain.Installations.GameInstallation).Assembly];
        yield return [typeof(Application.Common.Abstractions.IAppStorageLocations).Assembly];
        yield return [typeof(Infrastructure.Common.Storage.AppStorageLocations).Assembly];
        yield return [typeof(Erdmier.ZooTycoonLauncher.Desktop.App).Assembly];
    }

    [Theory]
    [MemberData(nameof(SourceProjectAssemblies))]
    public void EveryPublicTypeHasAFileNamedAfterIt(System.Reflection.Assembly assembly)
    {
        string assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;
        string projectDirectory = ResolveProjectDirectoryFromBinPath(assemblyDirectory, assembly.GetName().Name!);

        IEnumerable<Type> publicTypes = assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested && !t.IsCompilerGenerated())
            .Where(t => t.Namespace is null || !t.Namespace.EndsWith(".Migrations", StringComparison.Ordinal)); // EF Core tooling owns Migration filenames.

        List<string> violations = new();
        foreach (Type type in publicTypes)
        {
            string baseName = StripGenericArity(type.Name);

            // Avalonia XAML code-behind files are named TypeName.axaml.cs rather than TypeName.cs.
            string[] matches = Directory.GetFiles(projectDirectory, baseName + ".cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(projectDirectory, baseName + ".axaml.cs", SearchOption.AllDirectories))
                .ToArray();

            if (matches.Length == 0)
            {
                violations.Add($"{type.FullName} — no file named {baseName}.cs or {baseName}.axaml.cs in {projectDirectory}");
            }
        }

        violations.ShouldBeEmpty(BuildFailureMessage(violations));
    }

    private static string ResolveProjectDirectoryFromBinPath(string binDir, string assemblyName)
    {
        DirectoryInfo? current = new DirectoryInfo(binDir);
        while (current is not null)
        {
            string sourceCandidate = Path.Combine(current.FullName, "Source", assemblyName);
            if (Directory.Exists(sourceCandidate) && File.Exists(Path.Combine(sourceCandidate, assemblyName + ".csproj")))
            {
                return sourceCandidate;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate project directory for {assemblyName} starting from {binDir}.");
    }

    private static string StripGenericArity(string name)
    {
        int tickIndex = name.IndexOf('`');
        return tickIndex < 0 ? name : name[..tickIndex];
    }

    private static string BuildFailureMessage(IReadOnlyCollection<string> violations) =>
        violations.Count == 0
            ? string.Empty
            : $"One-type-per-file rule violated by:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", violations)}";
}

internal static class TypeExtensions
{
    public static bool IsCompilerGenerated(this Type type) =>
        type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false).Length > 0;
}
