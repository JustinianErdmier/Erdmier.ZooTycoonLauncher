using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Composition;

/// <summary>
///     Composition-graph guard: <c>AddInfrastructure</c> must register both the Serilog <see cref="Serilog.ILogger" /> singleton AND the
///     <c>Microsoft.Extensions.Logging</c> bridge (open-generic <see cref="ILogger{TCategoryName}" />) so handlers that take
///     <see cref="ILogger{TCategoryName}" /> (e.g. <c>LaunchGameHandler</c>) resolve from the container.
/// </summary>
public sealed class AddInfrastructureLoggingTests
{
    [ Fact ]
    public void AddInfrastructure_RegistersGenericIlogger_ForMicrosoftExtensionsLogging()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();

        using ServiceProvider provider = services.BuildServiceProvider();

        ILogger<AddInfrastructureLoggingTests> generic =
            provider.GetRequiredService<ILogger<AddInfrastructureLoggingTests>>();

        generic.ShouldNotBeNull();
    }

    [ Fact ]
    public void AddInfrastructure_RegistersSerilogILogger_ForLegacyConsumers()
    {
        ServiceCollection services = new();

        services.AddInfrastructure();

        using ServiceProvider provider = services.BuildServiceProvider();

        Serilog.ILogger serilog = provider.GetRequiredService<Serilog.ILogger>();

        serilog.ShouldNotBeNull();
    }
}
