namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Configurations;

/// <summary>
/// EF Core configuration for <see cref="LauncherSettings" /> — single row enforced by a <c>CHECK</c> constraint.
/// </summary>
[UsedImplicitly]
public sealed class LauncherSettingsConfiguration : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<LauncherSettings>
{
    /// <inheritdoc />
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LauncherSettings> builder)
    {
        builder.ToTable("LauncherSettings", b => b.HasCheckConstraint("CK_LauncherSettings_SingletonRow", "\"Id\" = 1"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.LauncherStartupPreference)
               .HasConversion(
                    static v => v.Name,
                    static v => LauncherStartupPreference.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.CloseAfterGameLaunch).IsRequired();
        builder.Property(s => s.DefaultInstallationId);

        builder.Property(s => s.Theme)
               .HasConversion(
                    static v => v.Name,
                    static v => LauncherTheme.FromName(v, ignoreCase: false))
               .HasDefaultValue(LauncherTheme.System)
               .IsRequired();
    }
}
