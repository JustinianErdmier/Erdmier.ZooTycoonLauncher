namespace Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;

/// <summary>
/// Opaque handle around an open per-installation context. Application code keeps the handle alive only as long as it needs the
/// context; dispose releases the underlying EF resources.
/// </summary>
public interface IInstallationDbContextHandle : IAsyncDisposable;
