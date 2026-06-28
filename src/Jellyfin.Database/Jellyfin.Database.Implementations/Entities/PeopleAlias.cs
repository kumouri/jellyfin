using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents an alternate name (alias / AKA) for a <see cref="Entities.People"/> entity.
/// </summary>
public class PeopleAlias
{
    /// <summary>
    /// Gets or sets the alias id.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the person this alias belongs to.
    /// </summary>
    public required Guid PeopleId { get; set; }

    /// <summary>
    /// Gets or sets the person this alias belongs to.
    /// </summary>
    public People? People { get; set; }

    /// <summary>
    /// Gets or sets the alias as entered by the user (display form).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Gets or sets the normalized alias (lowercased, diacritics-stripped) used for matching.
    /// </summary>
    public required string AliasNormalized { get; set; }
}
