// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Extensions.Caching
{
    using System.Text.Json;

    /// <summary>
    /// Configuration values for a CacheManager instance.
    /// </summary>
    public class CacheManagerOptions
    {
        /// <summary>
        /// Gets or sets the serializer options for serializing objects to and from the cache.
        /// </summary>
        public JsonSerializerOptions? SerializerOptions { get; set; }
    }
}
