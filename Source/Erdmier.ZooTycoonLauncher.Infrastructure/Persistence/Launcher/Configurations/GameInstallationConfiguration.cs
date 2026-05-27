using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>EF Core configuration for <see cref="GameInstallation" /> — case-insensitive uniqueness on <c>Name</c> and <c>Path</c>.</summary>
public sealed class GameInstallationConfiguration : IEntityTypeConfiguration<GameInstallation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GameInstallation> builder)
    {
        builder.ToTable(name: "GameInstallations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .ValueGeneratedNever();

        builder.Property(i => i.Name)
               .IsRequired()
               .UseCollation(collation: "NOCASE");

        builder.Property(i => i.Path)
               .IsRequired()
               .UseCollation(collation: "NOCASE");

        builder.Property(i => i.HasExe)
               .IsRequired();

        builder.Property(i => i.HasIni)
               .IsRequired();

        builder.Property(i => i.AddedUtc)
               .IsRequired();

        builder.Property(i => i.ModifiedUtc);

        builder.Property(i => i.LastPlayedUtc);

        builder.Property(i => i.LastOpenedUtc);

        builder.HasIndex(i => i.Name)
               .IsUnique();

        builder.HasIndex(i => i.Path)
               .IsUnique();

        builder.Ignore(i => i.Validity);
    }
}
