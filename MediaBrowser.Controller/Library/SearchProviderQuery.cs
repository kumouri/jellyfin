using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Query object for search providers.
/// </summary>
public class SearchProviderQuery
{
    /// <summary>
    /// Gets the search term.
    /// </summary>
    public required string SearchTerm { get; init; }

    /// <summary>
    /// Gets the user ID for user-specific searches.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the item types to include in the search.
    /// </summary>
    public BaseItemKind[] IncludeItemTypes { get; init; } = [];

    /// <summary>
    /// Gets the item types to exclude from the search.
    /// </summary>
    public BaseItemKind[] ExcludeItemTypes { get; init; } = [];

    /// <summary>
    /// Gets the media types to include in the search.
    /// </summary>
    public MediaType[] MediaTypes { get; init; } = [];

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the parent ID to scope the search.
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="SearchTerm"/> should be treated as a .NET
    /// regular expression and matched in memory rather than as a normalized substring.
    /// </summary>
    public bool IsRegex { get; init; }

    /// <summary>
    /// Gets a value indicating whether regex matching is case-insensitive. Only used when
    /// <see cref="IsRegex"/> is <c>true</c>.
    /// </summary>
    public bool RegexIgnoreCase { get; init; } = true;
}
