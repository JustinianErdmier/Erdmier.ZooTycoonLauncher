using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Tests.Integration.Persistence;

public sealed class InstallationRepositoryTests : IDisposable
{
    private readonly LauncherDbContext _context;

    private readonly string _databasePath;

    private readonly InstallationRepository _repository;

    public InstallationRepositoryTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"zoolauncher-repo-{Guid.NewGuid()}.db");

        DbContextOptions<LauncherDbContext> options = new DbContextOptionsBuilder<LauncherDbContext>()
                                                      .UseSqlite($"Data Source={_databasePath}")
                                                      .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                                                      .Options;

        _context = new LauncherDbContext(options);
        _context.Database.Migrate();

        _repository = new InstallationRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [ Fact ]
    public async Task AddAsync_PersistsRow()
    {
        GameInstallation installation = NewInstallation(name: "Main", path: @"C:\Games\Zoo Tycoon");

        await _repository.AddAsync(installation, CancellationToken.None);

        GameInstallation? read = await _repository.GetByIdAsync(installation.Id, CancellationToken.None);
        read.ShouldNotBeNull();
        read.Name.ShouldBe(expected: "Main");
    }

    [ Fact ]
    public async Task GetAllAsync_ReturnsRowsAlphabeticallyCaseInsensitive()
    {
        await _repository.AddAsync(NewInstallation(name: "zebra", path: @"C:\Games\A-zebra"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "Antelope", path: @"C:\Games\B-antelope"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "buffalo", path: @"C:\Games\C-buffalo"), CancellationToken.None);

        IReadOnlyList<GameInstallation> all = await _repository.GetAllAsync(CancellationToken.None);

        all.Select(i => i.Name)
           .ShouldBe(["Antelope", "buffalo", "zebra"]);
    }

    [ Fact ]
    public async Task ExistsByNameAsync_IsCaseInsensitive_AndHonoursExcludeId()
    {
        GameInstallation main = NewInstallation(name: "Main", path: @"C:\Games\Main");
        await _repository.AddAsync(main, CancellationToken.None);

        (await _repository.ExistsByNameAsync(name: "main", excludeId: null, CancellationToken.None)).ShouldBeTrue();
        (await _repository.ExistsByNameAsync(name: "MAIN", main.Id, CancellationToken.None)).ShouldBeFalse();
        (await _repository.ExistsByNameAsync(name: "Other", excludeId: null, CancellationToken.None)).ShouldBeFalse();
    }

    [ Fact ]
    public async Task ExistsByPathAsync_IsCaseInsensitive_AndHonoursExcludeId()
    {
        GameInstallation main = NewInstallation(name: "Main", path: @"C:\Games\Main");
        await _repository.AddAsync(main, CancellationToken.None);

        (await _repository.ExistsByPathAsync(path: @"c:\games\main", excludeId: null, CancellationToken.None)).ShouldBeTrue();
        (await _repository.ExistsByPathAsync(path: @"C:\GAMES\MAIN", main.Id, CancellationToken.None)).ShouldBeFalse();
        (await _repository.ExistsByPathAsync(path: @"C:\Games\Other", excludeId: null, CancellationToken.None)).ShouldBeFalse();
    }

    [ Fact ]
    public async Task UpdateAsync_PersistsMutableFields()
    {
        GameInstallation row = NewInstallation(name: "Original", path: @"C:\Games\Original");
        await _repository.AddAsync(row, CancellationToken.None);

        row.Name        = "Renamed";
        row.HasExe      = false;
        row.ModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(row, CancellationToken.None);

        GameInstallation? read = await _repository.GetByIdAsync(row.Id, CancellationToken.None);
        read.ShouldNotBeNull();
        read.Name.ShouldBe(expected: "Renamed");
        read.HasExe.ShouldBeFalse();
        read.ModifiedUtc.ShouldNotBeNull();
    }

    [ Fact ]
    public async Task DeleteAsync_RemovesRow_AndIsIdempotent()
    {
        GameInstallation row = NewInstallation(name: "Doomed", path: @"C:\Games\Doomed");
        await _repository.AddAsync(row, CancellationToken.None);

        await _repository.DeleteAsync(row.Id, CancellationToken.None);
        (await _repository.GetByIdAsync(row.Id, CancellationToken.None)).ShouldBeNull();

        await Should.NotThrowAsync(async () => await _repository.DeleteAsync(row.Id, CancellationToken.None));
    }

    [ Fact ]
    public async Task FindDefaultPromotionCandidateAsync_ReturnsAlphabeticallyFirstByCaseInsensitiveName()
    {
        await _repository.AddAsync(NewInstallation(name: "zebra", path: @"C:\Games\zebra"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "antelope", path: @"C:\Games\antelope"), CancellationToken.None);
        await _repository.AddAsync(NewInstallation(name: "Buffalo", path: @"C:\Games\Buffalo"), CancellationToken.None);

        GameInstallation? winner = await _repository.FindDefaultPromotionCandidateAsync(CancellationToken.None);

        winner.ShouldNotBeNull();
        winner.Name.ShouldBe(expected: "antelope");
    }

    [ Fact ]
    public async Task FindDefaultPromotionCandidateAsync_ReturnsNullWhenTableEmpty()
    {
        GameInstallation? winner = await _repository.FindDefaultPromotionCandidateAsync(CancellationToken.None);
        winner.ShouldBeNull();
    }

    private static GameInstallation NewInstallation(string name, string path)
        => new()
        {
            Id       = Guid.CreateVersion7(),
            Name     = name,
            Path     = path,
            HasExe   = true,
            HasIni   = true,
            AddedUtc = DateTime.UtcNow
        };
}
