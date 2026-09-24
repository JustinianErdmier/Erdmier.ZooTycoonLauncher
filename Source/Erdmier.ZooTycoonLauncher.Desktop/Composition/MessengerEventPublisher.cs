namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     The CommunityToolkit-messenger implementation of <see cref="IApplicationEventPublisher" />. Sends on the UI thread — immediately when already on it, otherwise
///     posted — so recipients such as <see cref="InstallationGridViewModel" /> never observe a message off the UI thread.
/// </summary>
internal sealed class MessengerEventPublisher : IApplicationEventPublisher
{
    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="messenger">The application-wide messenger.</param>
    public MessengerEventPublisher(IMessenger messenger) => _messenger = messenger;

    /// <inheritdoc />
    public void Publish<TMessage>(TMessage message)
        where TMessage : class
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _messenger.Send(message);

            return;
        }

        Dispatcher.UIThread.Post(() => _messenger.Send(message));
    }
}
