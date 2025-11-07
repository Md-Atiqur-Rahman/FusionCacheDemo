using Domain.CachingService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Services.CachingService
{
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(
            IFusionCache cache,
            ILogger<CacheInvalidationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task InvalidateProductCachesAsync(string? category = null)
        {
            await _cache.RemoveAsync("products:all");
            await _cache.RemoveAsync("products:count");

            if (!string.IsNullOrEmpty(category))
            {
                var categoryCacheKey = $"products:category:{category.ToLowerInvariant()}";
                await _cache.RemoveAsync(categoryCacheKey);

                _logger.LogInformation("Invalidated product caches for category: {Category}", category);
            }
            else
            {
                _logger.LogInformation("Invalidated all product list caches");
            }
        }

        public async Task InvalidateAllCachesAsync()
        {
            // FusionCache doesn't have built-in pattern deletion
            // You would need to track keys or use Redis SCAN if needed
            _logger.LogWarning("Full cache clear not implemented - invalidating known keys");

            await _cache.RemoveAsync("products:all");
            await _cache.RemoveAsync("products:count");

            _logger.LogInformation("Invalidated all known caches");
        }

        public Task InvalidateByPatternAsync(string pattern)
        {
            // For pattern-based invalidation, you'd need to either:
            // 1. Track keys in a separate store
            // 2. Use Redis SCAN directly (bypassing FusionCache)
            // 3. Use FusionCache tags feature (advanced)

            _logger.LogWarning(
                "Pattern-based invalidation not implemented for: {Pattern}",
                pattern);

            return Task.CompletedTask;
        }
    }
}
