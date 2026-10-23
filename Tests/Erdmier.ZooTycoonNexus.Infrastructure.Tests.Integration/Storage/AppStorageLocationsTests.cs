using Erdmier.ZooTycoonNexus.Infrastructure.Common.Storage;

namespace Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration.Storage;

/// <summary>Verifies that <see cref="AppStorageLocations" /> resolves every path under the <c>ZooTycoonNexus</c> app-data folder.</summary>
public sealed class AppStorageLocationsTests
{
    [ Fact ]
    public void Paths_LiveUnderTheZooTycoonNexusFolder()
    {
        Guid                id        = Guid.NewGuid();
        AppStorageLocations locations = new(new MockFileSystem());

        locations.AppDataRoot.ShouldBe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZooTycoonNexus"));
        locations.LauncherDatabasePath.ShouldBe(Path.Combine(locations.AppDataRoot, "Data", "Nexus.db"));
        locations.LauncherLogPath.ShouldBe(Path.Combine(locations.AppDataRoot, "Logs", "Nexus.log"));
        locations.InstallationDatabasePath(id).ShouldBe(Path.Combine(locations.AppDataRoot, "Data", $"{id}.db"));
        locations.InstallationLogPath(id).ShouldBe(Path.Combine(locations.AppDataRoot, "Logs", "Installations", $"{id}.log"));
    }

    [ Fact ]
    public void Construction_CreatesTheFoldersInTheGivenFileSystemOnly()
    {
        MockFileSystem      fileSystem = new();
        AppStorageLocations locations  = new(fileSystem);

        fileSystem.Directory.Exists(Path.Combine(locations.AppDataRoot, "Data")).ShouldBeTrue();
        fileSystem.Directory.Exists(Path.Combine(locations.AppDataRoot, "Logs")).ShouldBeTrue();
        fileSystem.Directory.Exists(Path.Combine(locations.AppDataRoot, "Logs", "Installations")).ShouldBeTrue();
        fileSystem.AllDirectories.ShouldNotContain(directory => directory.Contains("ZooTycoonLauncher"));
    }
}
