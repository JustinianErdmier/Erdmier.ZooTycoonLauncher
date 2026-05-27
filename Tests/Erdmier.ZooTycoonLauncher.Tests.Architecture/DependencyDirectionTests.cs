using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
using Erdmier.ZooTycoonLauncher.Desktop;
using Erdmier.ZooTycoonLauncher.Domain.Installations;

namespace Erdmier.ZooTycoonLauncher.Tests.Architecture;

public sealed class DependencyDirectionTests
{
    private const string ApplicationAssembly = "Erdmier.ZooTycoonLauncher.Application";

    private const string DomainAssembly = "Erdmier.ZooTycoonLauncher.Domain";

    private const string InfrastructureAssembly = "Erdmier.ZooTycoonLauncher.Infrastructure";

    [ Fact ]
    public void Domain_DoesNotReferenceApplicationOrInfrastructure()
    {
        TestResult result = Types.InAssembly(typeof(GameInstallation).Assembly)
                                 .ShouldNot()
                                 .HaveDependencyOnAny(ApplicationAssembly, InfrastructureAssembly)
                                 .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    [ Fact ]
    public void Application_DoesNotReferenceInfrastructure()
    {
        TestResult result = Types.InAssembly(typeof(IAppStorageLocations).Assembly)
                                 .ShouldNot()
                                 .HaveDependencyOn(InfrastructureAssembly)
                                 .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    [ Fact ]
    public void Application_DoesNotReferenceEfCoreOrSerilogOrAvalonia()
    {
        TestResult result = Types.InAssembly(typeof(IAppStorageLocations).Assembly)
                                 .ShouldNot()
                                 .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Serilog", "Avalonia")
                                 .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    [ Fact ]
    public void Desktop_OnlyReferencesInfrastructureFromCompositionNamespace()
    {
        TestResult result = Types.InAssembly(typeof(App).Assembly)
                                 .That()
                                 .DoNotResideInNamespace(name: "Erdmier.ZooTycoonLauncher.Desktop.Composition")
                                 .ShouldNot()
                                 .HaveDependencyOn(InfrastructureAssembly)
                                 .GetResult();

        result.IsSuccessful.ShouldBeTrue(BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(TestResult result)
        => result.FailingTypeNames is null
               ? "Dependency-direction rule violated."
               : $"Dependency-direction rule violated by:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", result.FailingTypeNames)}";
}
