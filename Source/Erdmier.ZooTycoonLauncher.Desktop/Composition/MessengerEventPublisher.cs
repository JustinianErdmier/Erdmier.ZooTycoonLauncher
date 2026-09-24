namespace Erdmier.ZooTycoonLauncher.Desktop.Composition;

/// <summary>
///     The CommunityToolkit-messenger implementation of <see cref="IApplicationEventPublisher" />. Always posts to the UI thread — never sending inline, even when
///     already on it — and catches any exception a recipient's <c>Receive</c> throws, logging it rather than letting it propagate. <see cref="WeakReferenceMessenger" />
///     does not itself guard against a throwing recipient, so this class is the boundary that keeps a subscriber's failure from surfacing back into the Application
///     handler that just published (SDD §7.2; the message has already been persisted by the time it is sent).
/// </summary>
internal sealed class MessengerEventPublisher : IApplicationEventPublisher
{
    private readonly ILogger<MessengerEventPublisher> _logger;

    private readonly IMessenger _messenger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="messenger">The application-wide messenger.</param>
    /// <param name="logger">Logger for recipient failures, which are caught here rather than thrown back into the publishing handler.</param>
    public MessengerEventPublisher(IMessenger messenger, ILogger<MessengerEventPublisher> logger)
    {
        _messenger = messenger;
        _logger    = logger;
    }

    /// <inheritdoc />
    public void Publish<TMessage>(TMessage message)
        where TMessage : class
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _messenger.Send(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver {MessageType} to its subscribers.", typeof(TMessage).Name);
            }
        });
    }
}
