#nullable disable

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides methods for accessing Peoples.
/// </summary>
public interface IPeopleRepository
{
    /// <summary>
    /// Gets the people.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>The list of people matching the filter.</returns>
    QueryResult<PersonInfo> GetPeople(InternalPeopleQuery filter);

    /// <summary>
    /// Updates the people.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="people">The people.</param>
    void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people);

    /// <summary>
    /// Gets the people names.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>The list of people names matching the filter.</returns>
    IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter);

    /// <summary>
    /// Gets the aliases (alternate names) for a person, resolved by name.
    /// </summary>
    /// <param name="personName">The person's name.</param>
    /// <returns>The distinct aliases for the person, or an empty list.</returns>
    IReadOnlyList<string> GetAliases(string personName);

    /// <summary>
    /// Replaces the aliases for a person, resolved by name. This is the only write path for
    /// aliases; it is intentionally separate from <see cref="UpdatePeople"/> so the per-item cast
    /// rewrite that runs on every metadata refresh never touches user-entered aliases.
    /// </summary>
    /// <param name="personName">The person's name. Aliases are applied to every people row sharing this name.</param>
    /// <param name="aliases">The complete desired set of aliases.</param>
    void UpdateAliases(string personName, IReadOnlyList<string> aliases);

    /// <summary>
    /// Gets the timed tags for a person, resolved by name.
    /// </summary>
    /// <param name="personName">The person's name.</param>
    /// <returns>The tags for the person (name + optional start/end dates), or an empty list.</returns>
    IReadOnlyList<(string Tag, DateTime? StartDate, DateTime? EndDate)> GetTags(string personName);

    /// <summary>
    /// Replaces the timed tags for a person, resolved by name. Separate from <see cref="UpdatePeople"/>
    /// so metadata refreshes never touch user-entered tags.
    /// </summary>
    /// <param name="personName">The person's name. Tags are applied to every people row sharing this name.</param>
    /// <param name="tags">The complete desired set of tags (name + optional start/end dates).</param>
    void UpdateTags(string personName, IReadOnlyList<(string Tag, DateTime? StartDate, DateTime? EndDate)> tags);

    /// <summary>
    /// Gets the distinct tag names used across all people.
    /// </summary>
    /// <returns>The distinct tag names, ordered alphabetically.</returns>
    IReadOnlyList<string> GetAllTagNames();

    /// <summary>
    /// Gets the distinct people names per item for multiple items efficiently by querying from the mapping table.
    /// </summary>
    /// <param name="itemIds">The item IDs to get people for.</param>
    /// <param name="personTypes">The person types to include (e.g. "Actor", "Director").</param>
    /// <returns>A dictionary mapping each item ID to its distinct people names, ordered by cast list order. Items with no matching people are omitted.</returns>
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> GetPeopleNamesByItems(IReadOnlyList<Guid> itemIds, IReadOnlyList<string> personTypes);
}
