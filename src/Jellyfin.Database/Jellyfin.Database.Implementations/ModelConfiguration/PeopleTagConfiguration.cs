using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// People tag configuration.
/// </summary>
public class PeopleTagConfiguration : IEntityTypeConfiguration<PeopleTag>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<PeopleTag> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.PeopleId);

        // Search matching. No uniqueness: the same tag may recur with different date windows
        // (e.g. an attribute that was true across two separate periods).
        builder.HasIndex(e => e.TagNormalized);

        builder.HasOne(e => e.People)
            .WithMany(e => e.Tags)
            .HasForeignKey(e => e.PeopleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
