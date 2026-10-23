namespace Erdmier.ZooTycoonNexus.Application.IniConfig.Save;

/// <summary>
///     Handler for <see cref="SaveIniCommand" />. Runs the shared <see cref="IniWritePipeline" /> (SDD §8.2) with a selector that keeps only the keys the user actually changed
///     from the reconciled on-disk values.
/// </summary>
public sealed class SaveIniHandler : ICommandHandler<SaveIniCommand, ErrorOr<IniConfigResult>>
{
    private readonly IniWritePipeline _pipeline;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="pipeline">The shared INI write pipeline.</param>
    public SaveIniHandler(IniWritePipeline pipeline) => _pipeline = pipeline;

    /// <inheritdoc />
    public async ValueTask<ErrorOr<IniConfigResult>> Handle(SaveIniCommand command, CancellationToken cancellationToken)
    {
        // The validator guarantees every key is recognised; resolving before the pipeline opens its transaction means this unreachable throw cannot be mislabelled
        // StoreFailed by the pipeline's catch. Normalising to the registry id gives inserted keys the registry's casing.
        Dictionary<IniKeyId, IniKeySpec> specs = [];

        foreach (IniKeyId id in command.Edits.Keys)
        {
            specs[id] = ZooIniDefaults.TryGet(id, out IniKeySpec? found) ? found : throw new InvalidOperationException($"Unrecognised key {id}.");
        }

        return await _pipeline.WriteAsync(command.InstallationId,
                                          (_, reconciledValues, _) =>
                                          {
                                              Dictionary<IniKeyId, string> edits = [];

                                              foreach ((IniKeyId id, string value) in command.Edits)
                                              {
                                                  IniKeySpec spec = specs[id];

                                                  if (!spec.AreEquivalent(value, reconciledValues.GetValueOrDefault(spec.Id)))
                                                  {
                                                      edits[spec.Id] = value;
                                                  }
                                              }

                                              return Task.FromResult<ErrorOr<IReadOnlyDictionary<IniKeyId, string>>>(edits);
                                          },
                                          cancellationToken);
    }
}
