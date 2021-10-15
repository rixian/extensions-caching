// Copyright (c) Rixian. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.

namespace Rixian.Extensions.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Rixian.Extensions.Errors;
    using static Rixian.Extensions.Errors.Prelude;

    /// <summary>
    /// Manages activities against the cache.
    /// </summary>
    public class CacheManager
    {
        /// <summary>
        /// The error code used in the event of a cache miss.
        /// </summary>
        public static readonly string CacheMissErrorCode = "cache.miss";

        /// <summary>
        /// The error code used in the event of an unknown value. For example: Unable to deserialize to a specific type.
        /// </summary>
        public static readonly string CacheUnknownValueErrorCode = "cache.unknown_value";

        private readonly IMemoryCache memoryCache;
        private readonly IDistributedCache distributedCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManager"/> class.
        /// </summary>
        /// <param name="memoryCache">Instace of the IMemoryCache interface.</param>
        /// <param name="distributedCache">Instace of the IDistributedCache interface.</param>
        public CacheManager(IMemoryCache memoryCache, IDistributedCache distributedCache)
        {
            this.memoryCache = memoryCache;
            this.distributedCache = distributedCache;
        }

        /// <summary>
        /// Removes the cache value with the given key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Optional. The System.Threading.CancellationToken used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            using Activity? activity = InternalUtil.ActivitySource.StartActivity("cache:remove");
            activity?.AddTag("key", key);

            this.memoryCache.Remove(key);
            await this.distributedCache.RemoveAsync(key, cancellationToken);
        }

        /// <summary>
        /// Refreshes the distributed cache value with the given key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Optional. The System.Threading.CancellationToken used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            using Activity? activity = InternalUtil.ActivitySource.StartActivity("cache:refresh");
            activity?.AddTag("key", key);

            await this.distributedCache.RefreshAsync(key, cancellationToken);
        }

        /// <summary>
        /// Gets the value from the cache or sets it if no value exists.
        /// </summary>
        /// <typeparam name="T">The object type to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cacheOptions">The cache options to use.</param>
        /// <param name="getValueAsync">Delegate to fetch the value to be cached in the event of a cache miss.</param>
        /// <param name="cancellationToken">Optional. The System.Threading.CancellationToken used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Throws if the getValueAsync delegate is null.</exception>
        public async Task<Result<T>> GetOrSetAsync<T>(string key, EntityCacheOptions cacheOptions, Func<CancellationToken, Task<Result<T>>> getValueAsync, CancellationToken cancellationToken = default)
        {
            using Activity? activity = InternalUtil.ActivitySource.StartActivity("cache:getorset");

            if (getValueAsync is null)
            {
                throw new ArgumentNullException(nameof(getValueAsync));
            }

            Result<T> result = await this.GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                activity?.AddEvent(new ActivityEvent("cache:got_value"));
                return result;
            }
            else
            {
                if (result.Error?.Code == CacheMissErrorCode)
                {
                    activity?.AddEvent(new ActivityEvent("cache:missed_value"));

                    Result<T> newValue = await getValueAsync(cancellationToken).ConfigureAwait(false);
                    activity?.AddEvent(new ActivityEvent("cache:fetched_current_value"));

                    if (newValue.IsSuccess && newValue.Value is { })
                    {
                        await this.SetAsync(key, newValue, cacheOptions, cancellationToken).ConfigureAwait(false);
                        activity?.AddEvent(new ActivityEvent("cache:set_cache_with_current_value"));
                    }

                    return newValue;
                }
                else
                {
                    return result;
                }
            }
        }

        /// <summary>
        /// Gets the value from the cache, otherwise returns an error.
        /// </summary>
        /// <typeparam name="T">The object type to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="cancellationToken">Optional. The System.Threading.CancellationToken used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            using Activity? activity = InternalUtil.ActivitySource.StartActivity("cache:get");
            activity?.AddTag("key", key);

            if (this.memoryCache.TryGetValue<T>(key, out T t))
            {
                activity?.AddEvent(new ActivityEvent("cache:got_from_memory"));
                return t;
            }
            else
            {
                activity?.AddEvent(new ActivityEvent("cache:miss_from_memory"));

                var content = await this.distributedCache.GetAsync(key, cancellationToken).ConfigureAwait(false);
                if (content == null)
                {
                    activity?.AddEvent(new ActivityEvent("cache:miss_from_remote"));
                    return Error(CacheMissErrorCode, "Cache miss.", key);
                }
                else
                {
                    activity?.AddEvent(new ActivityEvent("cache:got_from_remote"));
                    var json = Encoding.UTF8.GetString(content);
                    T? value = JsonSerializer.Deserialize<T>(json);

                    if (value is null)
                    {
                        activity?.AddEvent(new ActivityEvent("cache:deserialize_failed"));
                        return Error(CacheUnknownValueErrorCode, $"Unable to deserialize the cache value for key \"{key}\" to type \"{typeof(T)}\".");
                    }
                    else
                    {
                        return value;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value in the cache.
        /// </summary>
        /// <typeparam name="T">The object type to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="options">The cache options to use.</param>
        /// <param name="cancellationToken">Optional. The System.Threading.CancellationToken used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task SetAsync<T>(string key, T value, EntityCacheOptions? options = null, CancellationToken cancellationToken = default)
        {
            using Activity? activity = InternalUtil.ActivitySource.StartActivity("cache:set");
            activity?.AddTag("key", key);

            this.memoryCache.Set<T>(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = options?.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = options?.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = options?.SlidingExpiration,
            });
            activity?.AddEvent(new ActivityEvent("cache:set_memory"));

            var valueBytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await this.distributedCache.SetAsync(
                key,
                valueBytes,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = options?.AbsoluteExpiration,
                    AbsoluteExpirationRelativeToNow = options?.AbsoluteExpirationRelativeToNow,
                    SlidingExpiration = options?.SlidingExpiration,
                },
                cancellationToken).ConfigureAwait(false);
            activity?.AddEvent(new ActivityEvent("cache:set_remote"));
        }
    }
}
