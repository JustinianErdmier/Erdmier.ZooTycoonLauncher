namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>
/// EF Core configuration for <see cref="GameInstallation" /> — case-insensitive uniqueness on <c>Name</c> and <c>Path</c>.
/// </summary>
[UsedImplicitly]
public sealed class GameInstallationConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<GameInstallation>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<GameInstallation> builder)
    {
        builder.ToTable("GameInstallations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Name).IsRequired().UseCollation("NOCASE");
        builder.Property(i => i.Path).IsRequired().UseCollation("NOCASE");
        builder.Property(i => i.HasExe).IsRequired();
        builder.Property(i => i.HasIni).IsRequired();
        builder.Property(i => i.AddedUtc).IsRequired();
        builder.Property(i => i.ModifiedUtc);
        builder.Property(i => i.LastPlayedUtc);
        builder.Property(i => i.LastOpenedUtc);

        builder.HasIndex(i => i.Name).IsUnique();
        builder.HasIndex(i => i.Path).IsUnique();

        builder.Ignore(i => i.Validity);
    }
}
