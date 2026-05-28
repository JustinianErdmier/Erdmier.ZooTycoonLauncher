namespace Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation.Configurations;

/// <summary>
///     EF Core configuration for <see cref="IniValue" />. A row is identified by an auto-increment <c>long</c>; the <c>(SnapshotId, Section, Key)</c> triple is unique within a
///     snapshot.
/// </summary>
public sealed class IniValueConfiguration : IEntityTypeConfiguration<IniValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IniValue> builder)
    {
        builder.ToTable(name: "IniValues");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
               .ValueGeneratedOnAdd();

        builder.Property(v => v.SnapshotId)
               .IsRequired();

        builder.Property(v => v.Section)
               .IsRequired()
               .UseCollation(collation: "NOCASE");

        builder.Property(v => v.Key)
               .IsRequired()
               .UseCollation(collation: "NOCASE");

        builder.Property(v => v.Value);

        builder.Property(v => v.ValueKind)
               .HasConversion(static v => v.Name,
                              static v => IniValueKind.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.Property(v => v.Source)
               .HasConversion(static v => v.Name,
                              static v => IniValueSource.FromName(v, ignoreCase: false))
               .IsRequired();

        builder.HasIndex(v => new
               {
                   v.SnapshotId,
                   v.Section,
                   v.Key
               })
               .IsUnique();
    }
}
