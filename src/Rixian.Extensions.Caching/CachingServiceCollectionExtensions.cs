// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Rixian.Extensions.Caching;

    /// <summary>
    /// Extensions for the IServiceCollection interface to register caching dependencies.
    /// </summary>
    public static class CachingServiceCollectionExtensions
    {
        /// <summary>
        /// Registers dependencies with the IServiceCollection instance.
        /// </summary>
        /// <param name="services">The IServiceCollection instance to use.</param>
        /// <returns>The updated IServiceCollection instance.</returns>
        public static IServiceCollection AddManagedCaching(this IServiceCollection services)
        {
            services.AddTransient<CacheManager>();

            return services;
        }
    }
}
