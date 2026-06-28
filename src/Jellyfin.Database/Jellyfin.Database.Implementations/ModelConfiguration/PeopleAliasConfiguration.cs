using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// People alias configuration.
/// </summary>
public class PeopleAliasConfiguration : IEntityTypeConfiguration<PeopleAlias>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PeopleAlias> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.PeopleId);

        // Dedupe + case-insensitive uniqueness per person.
        builder.HasIndex(e => new { e.PeopleId, e.AliasNormalized }).IsUnique();

        // Search matching.
        builder.HasIndex(e => e.AliasNormalized);

        builder.HasOne(e => e.People)
            .WithMany(e => e.Aliases)
            .HasForeignKey(e => e.PeopleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
