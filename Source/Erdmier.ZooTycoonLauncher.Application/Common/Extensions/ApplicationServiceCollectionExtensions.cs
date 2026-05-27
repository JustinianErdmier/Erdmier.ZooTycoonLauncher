using Erdmier.ZooTycoonLauncher.Application.Common.Behaviours;
using FluentValidation;

namespace Erdmier.ZooTycoonLauncher.Application.Common.Extensions;

/// <summary>Composition-root extensions that register Application-layer services (Mediator + FluentValidation pipeline).</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <c>Mediator</c> (source-generated dispatcher), every <see cref="IValidator{T}" /> in the Application
    /// assembly, and the <see cref="ValidationBehaviour{TMessage,TResponse}" /> pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly, includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}
