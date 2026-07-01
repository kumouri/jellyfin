#nullable disable

#pragma warning disable CA2227, CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is a small Person stub that is attached to BaseItems.
    /// </summary>
    public sealed class PersonInfo : IHasProviderIds
    {
        public PersonInfo()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or Sets the PersonId.
        /// </summary>
        public Guid Id { get; set; }

        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        /// <value>The role.</value>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public PersonKind Type { get; set; }

        /// <summary>
        /// Gets or sets the ascending sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public int? SortOrder { get; set; }

        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the alternate names (aliases / AKA) for this person.
        /// </summary>
        /// <remarks>
        /// This is read-only output populated from the database for display/search. It is never
        /// written back through the per-item cast update path, so metadata refreshes can't wipe it.
        /// </remarks>
        public IReadOnlyList<string> Aliases { get; set; }

        /// <summary>
        /// Gets or sets the timed tags (optionally date-bounded) for this person. Read-only output.
        /// </summary>
        public IReadOnlyList<PersonTag> Tags { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        public bool IsType(PersonKind type) => Type == type || string.Equals(type.ToString(), Role, StringComparison.OrdinalIgnoreCase);
    }
}
