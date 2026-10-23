using Erdmier.ZooTycoonNexus.Application.Common.Behaviours;

namespace Erdmier.ZooTycoonNexus.Application.Common.Extensions;

/// <summary>
///     Composition-root extensions that register Application-layer services: Mediator, the FluentValidation validators and validation behaviour,
///     <see cref="IniReconciler" />, <see cref="IniWritePipeline" /> and <see cref="IIniSnapshotService" />.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <c>Mediator</c> (source-generated dispatcher), every <see cref="IValidator{T}" /> in the Application assembly, the
    ///     <see cref="ValidationBehaviour{TMessage,TResponse}" /> pipeline, <see cref="IniReconciler" />,
    ///     <see cref="IniWritePipeline" /> and <see cref="IIniSnapshotService" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options => { options.ServiceLifetime = ServiceLifetime.Scoped; });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddSingleton<IniReconciler>();
        services.AddScoped<IniWritePipeline>();
        services.AddScoped<IniRecoverySteps>();
        services.AddScoped<IIniSnapshotService, IniSnapshotService>();

        return services;
    }
}
