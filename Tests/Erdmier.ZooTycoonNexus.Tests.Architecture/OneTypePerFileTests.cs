using System.Reflection;
using System.Runtime.CompilerServices;

using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
using Erdmier.ZooTycoonNexus.Desktop;
using Erdmier.ZooTycoonNexus.Domain.Installations;
using Erdmier.ZooTycoonNexus.Infrastructure.Common.Storage;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture;

public sealed class OneTypePerFileTests
{
    [ Theory ]
    [ MemberData(nameof(SourceProjectAssemblies)) ]
    public void EveryPublicTypeHasAFileNamedAfterIt(Assembly assembly)
    {
        string projectDirectory = ProjectPaths.Project(assembly.GetName()
                                                       .Name!);

        IEnumerable<Type> publicTypes = assembly.GetTypes()
                                                .Where(t => t is { IsPublic: true, IsNested: false } && !t.IsCompilerGenerated())
                                                .Where(t => t.Namespace is not null
                                                            && t.Namespace.StartsWith(value: "Erdmier.ZooTycoonNexus",
                                                                                      StringComparison
                                                                                          .Ordinal)) // Source-generator artefacts (Mediator, EF) live in foreign namespaces — skip
                                                // them.
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
