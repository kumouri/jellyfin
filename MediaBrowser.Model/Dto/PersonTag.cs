#nullable disable

using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Represents a tag on a person, optionally bounded by a start and/or end date. A tag with no
    /// dates is always active; dates bound the period during which the tag applies.
    /// </summary>
    public class PersonTag
    {
        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the inclusive start of the tag's active window, or null for no lower bound.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the inclusive end of the tag's active window, or null for no upper bound.
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
