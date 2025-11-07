using Domain.CachingService;
using Domain.DistributedLockService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.CachingService
{
    /// <summary>
    /// Implements Cache-Aside (Lazy Loading) pattern with anti-stampede protection
    /// </summary>
    public class CacheAsideService
    {
        private readonly ICacheService _cacheService;
        private readonly IDistributedLockService _lockService;
        private readonly ILogger<CacheAsideService> _logger;
        private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(50);
        private const int MaxRetries = 20;

        public CacheAsideService(
            ICacheService cacheService,
            IDistributedLockService lockService,
            ILogger<CacheAsideService> logger)
        {
            _cacheService = cacheService;
            _lockService = lockService;
            _logger = logger;
        }

        /// <summary>
        /// Get or set with cache stampede prevention
        /// </summary>
        public async Task<T> GetOrSetWithStampedeProtectionAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan expiration)
        {
            // Try to get from cache
            var cachedValue = await _cacheService.GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Try to acquire lock
            var lockAcquired = await _lockService.AcquireLockAsync(key, LockExpiry);

            if (lockAcquired)
            {
                try
                {
                    // Double-check cache after acquiring lock
                    cachedValue = await _cacheService.GetAsync<T>(key);
                    if (cachedValue != null)
                    {
                        return cachedValue;
                    }

                    // Load from source
                    _logger.LogInformation("Loading data for key: {Key}", key);
                    var value = await factory();

                    // Store in cache
                    await _cacheService.SetAsync(key, value, expiration);

                    return value;
                }
                finally
                {
                    await _lockService.ReleaseLockAsync(key);
                }
            }
            else
            {
                // Wait for lock holder to populate cache
                for (int i = 0; i < MaxRetries; i++)
                {
                    await Task.Delay(LockRetryDelay);

                    cachedValue = await _cacheService.GetAsync<T>(key);
                    if (cachedValue != null)
                    {
                        return cachedValue;
                    }
                }

                // Fallback: load from source without caching
                _logger.LogWarning("Cache stampede protection timeout for key: {Key}", key);
                return await factory();
            }
        }
    }
}
