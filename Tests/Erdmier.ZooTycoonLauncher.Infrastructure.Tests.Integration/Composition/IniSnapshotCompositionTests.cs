using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
using Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;
using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Composition;

/// <summary>Composition guard: the real INI snapshot service and its seams resolve from the composed container, and the placeholder is gone.</summary>
public sealed class IniSnapshotCompositionTests
{
    [ Fact ]
    public void AddApplicationAndInfrastructure_ResolveTheRealIniServices()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();
        services.AddApplication();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope   scope    = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIniSnapshotService>().ShouldBeOfType<IniSnapshotService>();
        scope.ServiceProvider.GetRequiredService<IIniFileStore>().ShouldBeOfType<IniFileStore>();
        scope.ServiceProvider.GetRequiredService<IIniSnapshotRepository>().ShouldBeOfType<IniSnapshotRepository>();
        scope.ServiceProvider.GetRequiredService<IInstallationDbContextFactory>().ShouldBeOfType<InstallationDbContextFactory>();
    }

    [ Fact ]
    public void NullIniSnapshotService_NoLongerExists()
        => typeof(IniFileStore).Assembly.GetTypes().ShouldNotContain(type => type.Name == "NullIniSnapshotService");
}
