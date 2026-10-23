using System.IO.Abstractions;

using Microsoft.Extensions.Logging.Abstractions;

namespace Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration.Discovery;

public sealed class InstallationWriteProbeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoolauncher-probe-test-{Guid.NewGuid():N}");

    private readonly InstallationWriteProbe _probe = new(new FileSystem(), NullLogger<InstallationWriteProbe>.Instance);

    public InstallationWriteProbeTests() => Directory.CreateDirectory(_directory);

    private string IniPath => Path.Combine(_directory, path2: "zoo.ini");

    public void Dispose()
    {
        if (File.Exists(IniPath))
        {
            File.SetAttributes(IniPath, FileAttributes.Normal);
        }

        Directory.Delete(_directory, recursive: true);
    }

    [ Fact ]
    public async Task Probe_WritableFolder_IsWritable_AndLeavesNoFile()
    {
        (await _probe.ProbeAsync(_directory, CancellationToken.None)).ShouldBe(InstallationWriteAccess.Writable);

        Directory.GetFileSystemEntries(_directory).ShouldBeEmpty();
    }

    [ Fact ]
    public async Task Probe_ReadOnlyIniAttribute_IsFileReadOnly()
    {
        await File.WriteAllTextAsync(IniPath, contents: "x");

        File.SetAttributes(IniPath, FileAttributes.ReadOnly);

        try
        {
            (await _probe.ProbeAsync(_directory, CancellationToken.None)).ShouldBe(InstallationWriteAccess.FileReadOnly);
        }
        finally
        {
            File.SetAttributes(IniPath, FileAttributes.Normal);
        }
    }

    [ Fact ]
    public async Task Probe_MissingFolder_IsUnknown()
        => (await _probe.ProbeAsync(Path.Combine(_directory, path2: "missing"), CancellationToken.None)).ShouldBe(InstallationWriteAccess.Unknown);
}
