using System.Reflection;
using System.Runtime.CompilerServices;

using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
using Erdmier.ZooTycoonLauncher.Desktop;
using Erdmier.ZooTycoonLauncher.Domain.Installations;
using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Storage;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class OneTypePerFileTests
{
    [ Theory ]
    [ MemberData(nameof(SourceProjectAssemblies)) ]
    public void EveryPublicTypeHasAFileNamedAfterIt(Assembly assembly)
    {
        string assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;

        string projectDirectory = ResolveProjectDirectoryFromBinPath(assemblyDirectory,
                                                                     assembly.GetName()
                                                                             .Name!);

        IEnumerable<Type> publicTypes = assembly.GetTypes()
                                                .Where(t => t is { IsPublic: true, IsNested: false } && !t.IsCompilerGenerated())
                                                .Where(t => t.Namespace is not null
                                                            && t.Namespace.StartsWith(value: "Erdmier.ZooTycoonLauncher", StringComparison.Ordinal)) // Source-generator artefacts (Mediator, EF) live in foreign namespaces — skip them.
                                                .Where(t => !t.Namespace!.EndsWith(value: ".Migrations", StringComparison.Ordinal)); // EF Core tooling owns Migration filenames.

        List<string> violations =
        [
            .. from type in publicTypes
               let baseName = StripGenericArity(type.Name)
               let matches = Directory.GetFiles(projectDirectory, baseName + ".cs", SearchOption.AllDirectories)
                                      .Concat(Directory.GetFiles(projectDirectory, baseName + ".axaml.cs", SearchOption.AllDirectories))
                                      .ToArray()
               where matches.Length == 0
               select $"{type.FullName} — no file named {baseName}.cs or {baseName}.axaml.cs in {projectDirectory}"
        ];

        violations.ShouldBeEmpty(BuildFailureMessage(violations));
    }

    public static IEnumerable<object[]> SourceProjectAssemblies()
    {
        yield return [typeof(GameInstallation).Assembly];
        yield return [typeof(IAppStorageLocations).Assembly];
        yield return [typeof(AppStorageLocations).Assembly];
        yield return [typeof(App).Assembly];
    }

    private static string ResolveProjectDirectoryFromBinPath(string binDir, string assemblyName)
    {
        DirectoryInfo? current = new(binDir);

        while (current is not null)
        {
            string sourceCandidate = Path.Combine(current.FullName, path2: "Source", assemblyName);

            if (Directory.Exists(sourceCandidate)
                && File.Exists(Path.Combine(sourceCandidate, assemblyName + ".csproj")))
            {
                return sourceCandidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate project directory for {assemblyName} starting from {binDir}.");
    }

    private static string StripGenericArity(string name)
    {
        int tickIndex = name.IndexOf(value: '`');

        return tickIndex < 0 ? name : name[..tickIndex];
    }

    private static string BuildFailureMessage(IReadOnlyCollection<string> violations)
        => violations.Count == 0
               ? string.Empty
               : $"One-type-per-file rule violated by:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", violations)}";
}

internal static class TypeExtensions
{
    public static bool IsCompilerGenerated(this Type type)
        => type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false)
               .Length
           > 0;
}
