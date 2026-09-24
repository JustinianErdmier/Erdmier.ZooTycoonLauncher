using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;

namespace Erdmier.ZooTycoonLauncher.Desktop.Tests.Unit.Boot;

internal sealed class FakePendingChangesGuard(bool hasPendingChanges, bool allowLeave, Exception? throwOnConfirm = null) : ViewModelBase, IPendingChangesGuard
{
    public int Prompts { get; private set; }

    public bool HasPendingChanges { get; } = hasPendingChanges;

    public Task<bool> ConfirmLeaveAsync(CancellationToken cancellationToken)
    {
        Prompts++;

        if (throwOnConfirm is not null)
        {
            throw throwOnConfirm;
        }

        return Task.FromResult(allowLeave);
    }
}
