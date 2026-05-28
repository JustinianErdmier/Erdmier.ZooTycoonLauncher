namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Discovery;

public sealed class InstallationLocatorTests
{
    [ Fact ]
    public async Task LocateAsync_PrefersPersistedLastKnownPath()
    {
        MockFileSystem fs = new();
        fs.AddFile(path: @"C:\Persisted\Zoo\zoo.exe", new MockFileData([0x4D, 0x5A]));
        fs.AddFile(path: @"C:\Program Files\Microsoft Games\Zoo Tycoon\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: @"C:\Persisted\Zoo", CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(expected: @"C:\Persisted\Zoo");
    }

    [ Fact ]
    public async Task LocateAsync_FallsBackToProgramFiles_WhenPersistedPathInvalid()
    {
        MockFileSystem fs = new();
        fs.AddFile(path: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: null, CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(expected: @"C:\Program Files (x86)\Microsoft Games\Zoo Tycoon");
    }

    [ Fact ]
    public async Task LocateAsync_ReadsRegistry_WhenFilesystemPathsAbsent()
    {
        MockFileSystem fs = new();
        fs.AddFile(path: @"C:\Games\Custom\zoo.exe", new MockFileData([0x4D, 0x5A]));

        IRegistryReader registry = Substitute.For<IRegistryReader>();

        registry.ReadLocalMachineString(keyPath: @"SOFTWARE\Microsoft\Microsoft Games\Zoo Tycoon\1.0", valueName: "InstallPath")
                .Returns(returnThis: @"C:\Games\Custom");

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: null, CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Path.ShouldBe(expected: @"C:\Games\Custom");
    }

    [ Fact ]
    public async Task LocateAsync_ReturnsTrailWhenNothingFound()
    {
        MockFileSystem  fs       = new();
        IRegistryReader registry = Substitute.For<IRegistryReader>();

        InstallationLocator locator = new(fs, registry);

        LocatedDirectory result = await locator.LocateAsync(persistedLastKnownPath: @"C:\Persisted", CancellationToken.None);

        result.Found.ShouldBeFalse();
        result.Path.ShouldBeNull();
        result.Trail.ShouldNotBeEmpty();
        result.Trail.ShouldContain(a => a.Source == "Persisted last-known"       && a.Failure == LocationProbeFailure.DirectoryMissing);
        result.Trail.ShouldContain(a => a.Source.StartsWith("C:\\Program Files") && a.Failure == LocationProbeFailure.DirectoryMissing);
        result.Trail.ShouldContain(a => a.Source.StartsWith("HKLM\\")            && a.Failure == LocationProbeFailure.NoValue);
    }
}
