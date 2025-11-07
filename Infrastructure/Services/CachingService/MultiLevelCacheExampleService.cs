using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Services.CachingService
{
    /// <summary>
    /// Demonstrates multi-level caching with different strategies per level
    /// </summary>
    public class MultiLevelCacheExampleService
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<MultiLevelCacheExampleService> _logger;

        public MultiLevelCacheExampleService(
            IFusionCache cache,
            ILogger<MultiLevelCacheExampleService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Example: Different TTLs for L1 vs L2
        /// L1 (Memory): Short lived, fast access
        /// L2 (Redis): Longer lived, shared across servers
        /// </summary>
        public async Task<string> GetDataWithDifferentTTLsAsync(string key)
        {
            return await _cache.GetOrSetAsync(
                key,
                async ct =>
                {
                    _logger.LogInformation("Fetching data from source for key: {Key}", key);
                    await Task.Delay(1000); // Simulate expensive operation
                    return $"Data for {key} at {DateTime.UtcNow:HH:mm:ss}";
                },
                options =>
                {
                    // L1 (Memory): 2 minutes
                    options.Duration = TimeSpan.FromMinutes(2);

                    // L2 (Distributed): 30 minutes
                    options.DistributedCacheDuration = TimeSpan.FromMinutes(30);

                    // If L2 is slow, use soft timeout
                    options.DistributedCacheSoftTimeout = TimeSpan.FromMilliseconds(200);
                    options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(2);

                    _logger.LogDebug(
                        "Configured L1={L1}min, L2={L2}min for key: {Key}",
                        2, 30, key);
                });
        }

        /// <summary>
        /// Example: Background distributed cache operations
        /// Don't block the request waiting for Redis
        /// </summary>
        public async Task<string> GetDataWithBackgroundL2Async(string key)
        {
            return await _cache.GetOrSetAsync(
                key,
                async ct =>
                {
                    _logger.LogInformation("Factory execution for key: {Key}", key);
                    await Task.Delay(500);
                    return $"Data {key}";
                },
                options =>
                {
                    options.Duration = TimeSpan.FromMinutes(10);

                    // Don't wait for Redis operations
                    options.AllowBackgroundDistributedCacheOperations = true;

                    // This means Redis SET/GET happens in background
                    // Request completes immediately from L1 or factory
                    _logger.LogDebug("Background L2 operations enabled for: {Key}", key);
                });
        }
    }
}
