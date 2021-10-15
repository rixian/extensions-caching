// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace System
{
    using System.Diagnostics;

    /// <summary>
    /// Options for configuring the cache.
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class EntityCacheOptions
    {
        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before
        /// it will be removed. This will not extend the entry lifetime beyond the absolute
        /// expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        private string GetDebuggerDisplay()
        {
            return $"{nameof(this.AbsoluteExpiration)}={this.AbsoluteExpiration}, {nameof(this.AbsoluteExpirationRelativeToNow)}={this.AbsoluteExpirationRelativeToNow}, {nameof(this.SlidingExpiration)}={this.SlidingExpiration}, ";
        }
    }
}
