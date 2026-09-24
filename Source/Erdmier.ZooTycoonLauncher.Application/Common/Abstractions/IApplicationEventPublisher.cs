namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
///     Publishes installation-change notifications (the records in <c>Application.Common.Messaging</c>) to whichever layer composes the application. The Desktop
///     implementation forwards them to the CommunityToolkit messenger so the installation grids and the main window refresh (SDD §7.2.1–§7.2.4).
/// </summary>
public interface IApplicationEventPublisher
{
    /// <summary>Publishes <paramref name="message" /> to every current subscriber. Fire-and-forget: implementations must not throw into the caller.</summary>
    /// <param name="message">The message to publish.</param>
    /// <typeparam name="TMessage">The message type; subscribers register per concrete type.</typeparam>
    void Publish<TMessage>(TMessage message)
        where TMessage : class;
}
