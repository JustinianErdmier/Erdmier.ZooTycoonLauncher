namespace Erdmier.ZooTycoonLauncher.Application.Tests.Unit.Smoke;

public sealed class AssemblyLoadTests
{
    [Fact]
    public void ApplicationAssemblyLoadsAndExposesAbstractions()
    {
        Type abstraction = typeof(IAppStorageLocations);

        abstraction.Assembly.GetName().Name.ShouldBe("Erdmier.ZooTycoonLauncher.Application");
    }
}
