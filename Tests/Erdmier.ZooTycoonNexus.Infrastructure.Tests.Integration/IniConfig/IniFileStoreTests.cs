using System.IO.Abstractions;

namespace Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration.IniConfig;

public sealed class IniFileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"zoolauncher-ini-test-{Guid.NewGuid():N}");

    private readonly IniFileStore _store = new(new FileSystem(), Substitute.For<Serilog.ILogger>());

    public IniFileStoreTests() => Directory.CreateDirectory(_directory);

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
    public async Task Read_AbsentFile_ReturnsNull() => (await _store.ReadAsync(_directory, CancellationToken.None)).ShouldBeNull();

    [ Fact ]
    public async Task ReadThenWrite_RoundTripsAll256ByteValues()
    {
        byte[] original = Enumerable.Range(start: 0, count: 256).Select(value => (byte)value).ToArray();

        await File.WriteAllBytesAsync(IniPath, original);

        IniFileContent? content = await _store.ReadAsync(_directory, CancellationToken.None);

        content.ShouldNotBeNull();

        await _store.WriteAsync(_directory, content.Text, CancellationToken.None);

        (await File.ReadAllBytesAsync(IniPath)).ShouldBe(original);
    }

    [ Fact ]
    public async Task Write_LeavesNoTempFileAndReturnsTheLastWriteTime()
    {
        DateTime lastWrite = await _store.WriteAsync(_directory, text: "[user]\r\nfullscreen=0\r\n", CancellationToken.None);

        (await File.ReadAllTextAsync(IniPath)).ShouldBe(expected: "[user]\r\nfullscreen=0\r\n");
        lastWrite.ShouldBe(File.GetLastWriteTimeUtc(IniPath));
        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
    }

    [ Fact ]
    public async Task Write_OntoAReadOnlyFile_ThrowsAndRemovesTheTempFile()
    {
        await File.WriteAllTextAsync(IniPath, contents: "[user]\r\nfullscreen=1\r\n");

        File.SetAttributes(IniPath, FileAttributes.ReadOnly);

        await Should.ThrowAsync<UnauthorizedAccessException>(() => _store.WriteAsync(_directory, text: "changed", CancellationToken.None));

        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
        (await File.ReadAllTextAsync(IniPath)).ShouldBe(expected: "[user]\r\nfullscreen=1\r\n");
    }

    [ Fact ]
    public async Task DeleteOrphanedTempFiles_RemovesOnlyTempFiles()
    {
        await File.WriteAllTextAsync(IniPath, contents: "x");
        await File.WriteAllTextAsync(Path.Combine(_directory, path2: "zoo.ini.tmp.0123456789abcdef"), contents: "x");
        await File.WriteAllTextAsync(Path.Combine(_directory, path2: "zoo.ini.bak"), contents: "x");

        await _store.DeleteOrphanedTempFilesAsync(_directory, CancellationToken.None);

        Directory.GetFiles(_directory).Select(Path.GetFileName).Order().ShouldBe(["zoo.ini", "zoo.ini.bak"]);
    }

    [ Fact ]
    public async Task DeleteOrphanedTempFiles_MissingDirectory_DoesNothing()
        => await Should.NotThrowAsync(() => _store.DeleteOrphanedTempFilesAsync(Path.Combine(_directory, path2: "missing"), CancellationToken.None));

    [ Fact ]
    public async Task TryCreate_WritesTheTextWhenAbsent()
    {
        bool created = await _store.TryCreateAsync(_directory, text: "[user]\r\nname=caf\u00e9\r\n", CancellationToken.None);

        created.ShouldBeTrue();
        (await File.ReadAllBytesAsync(IniPath)).ShouldBe(System.Text.Encoding.Latin1.GetBytes("[user]\r\nname=caf\u00e9\r\n"));
        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
    }

    [ Fact ]
    public async Task TryCreate_ExistingFile_ReturnsFalseAndKeepsIt()
    {
        await File.WriteAllTextAsync(IniPath, contents: "original");

        bool created = await _store.TryCreateAsync(_directory, text: "replacement", CancellationToken.None);

        created.ShouldBeFalse();
        (await File.ReadAllTextAsync(IniPath)).ShouldBe(expected: "original");
        Directory.GetFiles(_directory, searchPattern: "zoo.ini.tmp.*").ShouldBeEmpty();
    }

    [ Fact ]
    public async Task ReadExternal_ReturnsLatin1Text()
    {
        string external = Path.Combine(_directory, path2: "other.ini");

        await File.WriteAllBytesAsync(external, [ 0x61, 0xE9, 0x62 ]);

        (await _store.ReadExternalAsync(external, maxBytes: 100, CancellationToken.None)).ShouldBe(expected: "a\u00e9b");
    }

    [ Fact ]
    public async Task ReadExternal_OverLimit_ReturnsNull()
    {
        string external = Path.Combine(_directory, path2: "other.ini");

        await File.WriteAllBytesAsync(external, [ 1, 2, 3, 4, 5 ]);

        (await _store.ReadExternalAsync(external, maxBytes: 4, CancellationToken.None)).ShouldBeNull();
    }
}
