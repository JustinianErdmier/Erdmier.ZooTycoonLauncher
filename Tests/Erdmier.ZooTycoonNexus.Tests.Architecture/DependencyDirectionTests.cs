using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
using Erdmier.ZooTycoonNexus.Desktop;
using Erdmier.ZooTycoonNexus.Domain.Installations;

namespace Erdmier.ZooTycoonNexus.Tests.Architecture;

public sealed class DependencyDirectionTests
{
    private const string ApplicationAssembly = "Erdmier.ZooTycoonNexus.Application";

    private const string DomainAssembly = "Erdmier.ZooTycoonNexus.Domain";

    private const string InfrastructureAssembly = "Erdmier.ZooTycoonNexus.Infrastructure";

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
                                 .DoNotResideInNamespace(name: "Erdmier.ZooTycoonNexus.Desktop.Composition")
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
