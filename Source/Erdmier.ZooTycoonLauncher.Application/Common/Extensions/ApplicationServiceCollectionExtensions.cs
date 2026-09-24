using Erdmier.ZooTycoonLauncher.Application.Common.Behaviours;

namespace Erdmier.ZooTycoonLauncher.Application.Common.Extensions;

/// <summary>Composition-root extensions that register Application-layer services (Mediator + FluentValidation pipeline).</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <c>Mediator</c> (source-generated dispatcher), every <see cref="IValidator{T}" /> in the Application assembly, the
    ///     <see cref="ValidationBehaviour{TMessage,TResponse}" /> pipeline, and the INI snapshot service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options => { options.ServiceLifetime = ServiceLifetime.Scoped; });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddSingleton<IniReconciler>();
        services.AddScoped<IIniSnapshotService, IniSnapshotService>();

        return services;
    }
}
