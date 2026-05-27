using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>EF Core configuration for <see cref="LauncherSettings" /> — single row enforced by a <c>CHECK</c> constraint.</summary>
public sealed class LauncherSettingsConfiguration : IEntityTypeConfiguration<LauncherSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LauncherSettings> builder)
    {
        builder.ToTable(name: "LauncherSettings", b => b.HasCheckConstraint(name: "CK_LauncherSettings_SingletonRow", sql: "\"Id\" = 1"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .ValueGeneratedNever();

        builder.Property(s => s.LauncherStartupPreference)
               .HasConversion(static v => v.Name,
                              static v => LauncherStartupPreference.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.CloseAfterGameLaunch)
               .IsRequired();

        builder.Property(s => s.DefaultInstallationId);

        builder.Property(s => s.Theme)
               .HasConversion(static v => v.Name,
                              static v => LauncherTheme.FromName(v, ignoreCase: false))
               .HasDefaultValue(LauncherTheme.System)
               .IsRequired();
    }
}
