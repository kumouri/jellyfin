using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents a tag applied to a <see cref="Entities.People"/> entity, optionally bounded by a
/// start and/or end date. A tag with no dates is always active; a tag with a start and/or end
/// date is only active within that window (used to model attributes that were only true during a
/// period of time).
/// </summary>
public class PeopleTag
{
    /// <summary>
    /// Gets or sets the tag id.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the person this tag belongs to.
    /// </summary>
    public required Guid PeopleId { get; set; }

    /// <summary>
    /// Gets or sets the person this tag belongs to.
    /// </summary>
    public People? People { get; set; }

    /// <summary>
    /// Gets or sets the tag as entered by the user (display form).
    /// </summary>
    public required string Tag { get; set; }

    /// <summary>
    /// Gets or sets the normalized tag (lowercased, diacritics-stripped) used for matching.
    /// </summary>
    public required string TagNormalized { get; set; }

    /// <summary>
    /// Gets or sets the inclusive start of the tag's active window, or null for no lower bound.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the inclusive end of the tag's active window, or null for no upper bound.
    /// </summary>
    public DateTime? EndDate { get; set; }
}
