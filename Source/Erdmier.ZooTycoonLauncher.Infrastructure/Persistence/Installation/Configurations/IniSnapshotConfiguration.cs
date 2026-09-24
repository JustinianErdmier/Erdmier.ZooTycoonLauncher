namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Configurations;

/// <summary>
///     EF Core configuration for <see cref="IniSnapshot" />. Each per-installation DB owns a single <c>Original</c> snapshot, a single <c>Current</c> snapshot, and zero or more
///     <c>Historical</c> snapshots; two partial unique indexes enforce at most one <c>Original</c> and one <c>Current</c>.
/// </summary>
public sealed class IniSnapshotConfiguration : IEntityTypeConfiguration<IniSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IniSnapshot> builder)
    {
        builder.ToTable(name: "Snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .ValueGeneratedNever();

        builder.Property(s => s.Kind)
               .HasConversion(static v => v.Name,
                              static v => IniSnapshotKind.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.Trigger)
               .HasConversion(static v => v.Name,
                              static v => IniSnapshotTrigger.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(s => s.CapturedUtc)
               .IsRequired();

        builder.Property(s => s.StructureBlob)
               .IsRequired();

        builder.HasIndex(s => s.Kind, name: "IX_Snapshots_Kind_Original")
               .IsUnique()
               .HasFilter(sql: "\"Kind\" = 'Original'");

        builder.HasIndex(s => s.Kind, name: "IX_Snapshots_Kind_Current")
               .IsUnique()
               .HasFilter(sql: "\"Kind\" = 'Current'");

        builder.HasIndex(s => new
               {
                   s.Kind,
                   s.CapturedUtc
               },
               name: "IX_Snapshots_Kind_CapturedUtc")
               .IsDescending(false, true);

        builder.HasMany(s => s.Values)
               .WithOne()
               .HasForeignKey(v => v.SnapshotId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
