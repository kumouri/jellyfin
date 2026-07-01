using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;
#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1307 // Specify StringComparison for clarity (these run as EF LINQ-to-SQL, not in-memory)
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

/// <summary>
/// Manager for handling people.
/// </summary>
/// <param name="dbProvider">Efcore Factory.</param>
/// <param name="itemTypeLookup">Items lookup service.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="PeopleRepository"/> class.
/// </remarks>
public class PeopleRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IItemTypeLookup itemTypeLookup) : IPeopleRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;

    /// <inheritdoc/>
    public QueryResult<PersonInfo> GetPeople(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        // Include PeopleBaseItemMap
        if (!filter.ItemId.IsEmpty())
        {
            dbQuery = dbQuery.Include(p => p.BaseItems!.Where(m => m.ItemId == filter.ItemId))
                .Include(p => p.Aliases)
                .Include(p => p.Tags)
                .OrderBy(e => e.BaseItems!.First(e => e.ItemId == filter.ItemId).ListOrder)
                .ThenBy(e => e.PersonType)
                .ThenBy(e => e.Name);
        }
        else
        {
            // The Peoples table has one row per (Name, PersonType), so the same person can
            // appear multiple times (e.g. as Actor and GuestStar). Collapse to one row per
            // name so /Persons doesn't return the same BaseItem id repeatedly. Lowercase the
            // grouping key so case-only duplicates collapse together.
            var representativeIds = dbQuery
                .GroupBy(e => e.Name.ToLower())
                .Select(g => g.Min(e => e.Id));
            dbQuery = context.Peoples.AsNoTracking()
                .Where(p => representativeIds.Contains(p.Id))
                .Include(p => p.Aliases)
                .Include(p => p.Tags)
                .OrderBy(e => e.Name);
        }

        var count = dbQuery.Count();
        if (filter.StartIndex.HasValue && filter.StartIndex > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return new QueryResult<PersonInfo>
        {
            StartIndex = filter.StartIndex ?? 0,
            TotalRecordCount = count,
            Items = dbQuery.AsEnumerable().Select(Map).ToArray(),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter).Select(e => e.Name).Distinct();

        if (filter.StartIndex.HasValue && filter.StartIndex > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.OrderBy(e => e).Take(filter.Limit);
        }

        return dbQuery.ToArray();
    }

    /// <inheritdoc />
    public void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people)
    {
        foreach (var person in people)
        {
            person.Name = person.Name.Trim();
            person.Role = person.Role?.Trim() ?? string.Empty;
        }

        // multiple metadata providers can provide the _same_ person; dedupe case-insensitively.
        people = people.DistinctBy(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();
        var personKeys = people.Select(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var existingPersons = context.Peoples.Select(e => new
        {
            item = e,
            SelectionKey = e.Name.ToLower() + "-" + e.PersonType
        })
            .Where(p => personKeys.Contains(p.SelectionKey))
            .Select(f => f.item)
            .ToArray();

        var toAdd = people
            .Where(e => !existingPersons.Any(f => string.Equals(f.Name, e.Name, StringComparison.OrdinalIgnoreCase) && f.PersonType == e.Type.ToString()))
            .Select(Map);
        context.Peoples.AddRange(toAdd);
        context.SaveChanges();

        var personsEntities = toAdd.Concat(existingPersons).ToArray();

        var existingMaps = context.PeopleBaseItemMap.Include(e => e.People).Where(e => e.ItemId == itemId).ToList();

        var listOrder = 0;

        foreach (var person in people)
        {
            var entityPerson = personsEntities.First(e => string.Equals(e.Name, person.Name, StringComparison.OrdinalIgnoreCase) && e.PersonType == person.Type.ToString());
            var existingMap = existingMaps.FirstOrDefault(e => string.Equals(e.People.Name, person.Name, StringComparison.OrdinalIgnoreCase) && e.People.PersonType == person.Type.ToString() && e.Role == person.Role);
            if (existingMap is null)
            {
                context.PeopleBaseItemMap.Add(new PeopleBaseItemMap()
                {
                    Item = null!,
                    ItemId = itemId,
                    People = null!,
                    PeopleId = entityPerson.Id,
                    ListOrder = listOrder,
                    SortOrder = person.SortOrder,
                    Role = person.Role
                });
            }
            else
            {
                // Update the order for existing mappings
                existingMap.ListOrder = listOrder;
                existingMap.SortOrder = person.SortOrder;
                // person mapping already exists so remove from list
                existingMaps.Remove(existingMap);
            }

            listOrder++;
        }

        context.PeopleBaseItemMap.RemoveRange(existingMaps);

        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Guid, IReadOnlyList<string>> GetPeopleNamesByItems(IReadOnlyList<Guid> itemIds, IReadOnlyList<string> personTypes)
    {
        using var context = _dbProvider.CreateDbContext();
        var query = context.PeopleBaseItemMap
            .AsNoTracking()
            .Where(m => itemIds.Contains(m.ItemId));

        if (personTypes.Count > 0)
        {
            query = query.Where(m => personTypes.Contains(m.People.PersonType));
        }

        var rows = query
            .OrderBy(m => m.ListOrder)
            .Select(m => new { m.ItemId, m.People.Name })
            .ToList();

        var result = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var group in rows.GroupBy(r => r.ItemId))
        {
            var names = group
                .Select(r => r.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToArray();

            if (names.Length > 0)
            {
                result[group.Key] = names;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAliases(string personName)
    {
        personName = personName?.Trim() ?? string.Empty;
        if (personName.Length == 0)
        {
            return Array.Empty<string>();
        }

        var nameLower = personName.ToLowerInvariant();
        using var context = _dbProvider.CreateDbContext();
        return context.PeopleAliases
            .AsNoTracking()
            .Where(a => context.Peoples.Any(p => p.Id == a.PeopleId && p.Name.ToLower() == nameLower))
            .Select(a => a.Alias)
            .Distinct()
            .ToArray();
    }

    /// <inheritdoc/>
    public void UpdateAliases(string personName, IReadOnlyList<string> aliases)
    {
        personName = personName?.Trim() ?? string.Empty;
        if (personName.Length == 0)
        {
            return;
        }

        var nameLower = personName.ToLowerInvariant();
        var ownNormalized = personName.GetCleanValue();

        // Normalize + dedupe; drop blanks and any alias equal to the person's own name.
        var desired = (aliases ?? Array.Empty<string>())
            .Select(a => (Alias: a?.Trim() ?? string.Empty, Normalized: (a?.Trim() ?? string.Empty).GetCleanValue()))
            .Where(a => a.Alias.Length > 0 && !string.IsNullOrEmpty(a.Normalized) && a.Normalized != ownNormalized)
            .GroupBy(a => a.Normalized)
            .Select(g => g.First())
            .ToArray();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        // Per-name keying: apply aliases to every people row sharing the name (e.g. the same
        // person credited as both Actor and GuestStar) so display + search stay consistent
        // regardless of which row a cast mapping points at.
        var personIds = context.Peoples
            .Where(p => p.Name.ToLower() == nameLower)
            .Select(p => p.Id)
            .ToArray();

        if (personIds.Length == 0)
        {
            return;
        }

        var existing = context.PeopleAliases
            .Where(a => personIds.Contains(a.PeopleId))
            .ToList();

        foreach (var personId in personIds)
        {
            var existingForPerson = existing.Where(e => e.PeopleId == personId).ToList();

            var toRemove = existingForPerson
                .Where(e => !desired.Any(d => d.Normalized == e.AliasNormalized))
                .ToList();
            context.PeopleAliases.RemoveRange(toRemove);

            var toAdd = desired
                .Where(d => !existingForPerson.Any(e => e.AliasNormalized == d.Normalized))
                .Select(d => new PeopleAlias
                {
                    Id = Guid.NewGuid(),
                    PeopleId = personId,
                    Alias = d.Alias,
                    AliasNormalized = d.Normalized
                });
            context.PeopleAliases.AddRange(toAdd);
        }

        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc/>
    public IReadOnlyList<(string Tag, DateTime? StartDate, DateTime? EndDate)> GetTags(string personName)
    {
        personName = personName?.Trim() ?? string.Empty;
        if (personName.Length == 0)
        {
            return Array.Empty<(string, DateTime?, DateTime?)>();
        }

        var nameLower = personName.ToLowerInvariant();
        using var context = _dbProvider.CreateDbContext();
        return context.PeopleTags
            .AsNoTracking()
            .Where(t => context.Peoples.Any(p => p.Id == t.PeopleId && p.Name.ToLower() == nameLower))
            .Select(t => new { t.Tag, t.StartDate, t.EndDate })
            .Distinct()
            .AsEnumerable()
            .Select(t => (t.Tag, t.StartDate, t.EndDate))
            .ToArray();
    }

    /// <inheritdoc/>
    public void UpdateTags(string personName, IReadOnlyList<(string Tag, DateTime? StartDate, DateTime? EndDate)> tags)
    {
        personName = personName?.Trim() ?? string.Empty;
        if (personName.Length == 0)
        {
            return;
        }

        var nameLower = personName.ToLowerInvariant();

        // Normalize + dedupe. The same tag text may recur with different date windows, so the
        // dedupe key includes the dates.
        var desired = (tags ?? Array.Empty<(string, DateTime?, DateTime?)>())
            .Select(t => new
            {
                Tag = t.Tag?.Trim() ?? string.Empty,
                t.StartDate,
                t.EndDate,
                Normalized = (t.Tag?.Trim() ?? string.Empty).GetCleanValue()
            })
            .Where(t => t.Tag.Length > 0 && !string.IsNullOrEmpty(t.Normalized))
            .GroupBy(t => new { t.Normalized, t.StartDate, t.EndDate })
            .Select(g => g.First())
            .ToArray();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        // Per-name keying: apply to every people row sharing the name (see UpdateAliases).
        var personIds = context.Peoples
            .Where(p => p.Name.ToLower() == nameLower)
            .Select(p => p.Id)
            .ToArray();

        if (personIds.Length == 0)
        {
            return;
        }

        // Replace-all: simplest correct behavior for a dated set.
        context.PeopleTags.RemoveRange(context.PeopleTags.Where(t => personIds.Contains(t.PeopleId)));

        foreach (var personId in personIds)
        {
            foreach (var t in desired)
            {
                context.PeopleTags.Add(new PeopleTag
                {
                    Id = Guid.NewGuid(),
                    PeopleId = personId,
                    Tag = t.Tag,
                    TagNormalized = t.Normalized,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate
                });
            }
        }

        context.SaveChanges();
        transaction.Commit();
    }

    private PersonInfo Map(People people)
    {
        var mapping = people.BaseItems?.FirstOrDefault();
        var personInfo = new PersonInfo()
        {
            Id = people.Id,
            Name = people.Name,
            Role = mapping?.Role,
            SortOrder = mapping?.SortOrder,
            Aliases = people.Aliases?.Select(a => a.Alias).ToArray() ?? Array.Empty<string>(),
            Tags = people.Tags?.Select(t => new PersonTag { Name = t.Tag, StartDate = t.StartDate, EndDate = t.EndDate }).ToArray() ?? Array.Empty<PersonTag>()
        };
        if (Enum.TryParse<PersonKind>(people.PersonType, out var kind))
        {
            personInfo.Type = kind;
        }

        return personInfo;
    }

    private People Map(PersonInfo people)
    {
        var personInfo = new People()
        {
            Name = people.Name,
            PersonType = people.Type.ToString(),
            Id = people.Id,
        };

        return personInfo;
    }

    private IQueryable<People> TranslateQuery(IQueryable<People> query, JellyfinDbContext context, InternalPeopleQuery filter)
    {
        if (filter.User is not null && filter.IsFavorite.HasValue)
        {
            var personType = itemTypeLookup.BaseItemKindNames[BaseItemKind.Person];
            var oldQuery = query;

            query = context.UserData
                .Where(u => u.Item!.Type == personType && u.IsFavorite == filter.IsFavorite && u.UserId.Equals(filter.User.Id))
                .Join(oldQuery, e => e.Item!.Name, e => e.Name, (item, person) => person)
                .Distinct()
                .AsNoTracking();
        }

        if (!filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId.Equals(filter.ItemId)));
        }

        if (filter.ParentId != null)
        {
            query = query.Where(e => e.BaseItems!.Any(w => context.AncestorIds.Any(i => i.ParentItemId == filter.ParentId && i.ItemId == w.ItemId)));
        }

        if (!filter.AppearsInItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId.Equals(filter.AppearsInItemId)));
        }

        var queryPersonTypes = filter.PersonTypes.Where(IsValidPersonType).ToList();
        if (queryPersonTypes.Count > 0)
        {
            query = query.Where(e => queryPersonTypes.Contains(e.PersonType));
        }

        var queryExcludePersonTypes = filter.ExcludePersonTypes.Where(IsValidPersonType).ToList();

        if (queryExcludePersonTypes.Count > 0)
        {
            query = query.Where(e => !queryExcludePersonTypes.Contains(e.PersonType));
        }

        if (filter.MaxListOrder.HasValue && !filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId == filter.ItemId && w.ListOrder <= filter.MaxListOrder.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var nameContainsUpper = filter.NameContains.ToUpper();
            var nameContainsClean = filter.NameContains.GetCleanValue();
            query = query.Where(e => e.Name.ToUpper().Contains(nameContainsUpper)
                || e.Aliases!.Any(a => a.AliasNormalized.Contains(nameContainsClean))
                || e.Tags!.Any(t => t.TagNormalized.Contains(nameContainsClean)));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            query = query.Where(e => e.Name.StartsWith(filter.NameStartsWith.ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            query = query.Where(e => e.Name.CompareTo(filter.NameLessThan.ToLowerInvariant()) < 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            query = query.Where(e => e.Name.CompareTo(filter.NameStartsWithOrGreater.ToLowerInvariant()) >= 0);
        }

        if (filter.Tags is { Count: > 0 })
        {
            var cleanTags = filter.Tags.Select(t => t.GetCleanValue()).ToArray();
            query = query.Where(e => e.Tags!.Any(t => cleanTags.Contains(t.TagNormalized)));
        }

        return query;
    }

    private bool IsAlphaNumeric(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        for (int i = 0; i < str.Length; i++)
        {
            if (!char.IsLetter(str[i]) && !char.IsNumber(str[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidPersonType(string value)
    {
        return IsAlphaNumeric(value);
    }
}
