using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Repositories
{
    // <summary>
    /// Decorator pattern: Wraps ProductRepository with FusionCache
    /// Implements advanced caching strategies with fail-safe and stampede protection
    /// </summary>
    public class CachedProductRepository : IProductRepository
    {
        private readonly IProductRepository _innerRepository;
        private readonly IFusionCache _cache;
        private readonly ILogger<CachedProductRepository> _logger;
        private readonly CacheSettings _cacheSettings;

        // Cache key prefixes following best practices
        private const string CACHE_KEY_PREFIX = "product:";
        private const string CACHE_KEY_ALL = "products:all";
        private const string CACHE_KEY_CATEGORY = "products:category:";
        private const string CACHE_KEY_SEARCH = "products:search:";
        private const string CACHE_KEY_COUNT = "products:count";

        public CachedProductRepository(
            IProductRepository innerRepository,
            IFusionCache cache,
            ILogger<CachedProductRepository> logger,
            IOptions<CacheSettings> cacheSettings)
        {
            _innerRepository = innerRepository;
            _cache = cache;
            _logger = logger;
            _cacheSettings = cacheSettings.Value;
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{id}";

            // FusionCache GetOrSetAsync with automatic stampede protection
            return await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    _logger.LogDebug("Cache MISS for product: {ProductId}", id);
                    return await _innerRepository.GetByIdAsync(id);
                },
                options => ConfigureEntryOptions(options, "GetById")
            );
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _cache.GetOrSetAsync(
                CACHE_KEY_ALL,
                async ct =>
                {
                    _logger.LogDebug("Cache MISS for all products");
                    return await _innerRepository.GetAllAsync();
                },
                options => ConfigureEntryOptions(options, "GetAll", TimeSpan.FromMinutes(5))
            );
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            var cacheKey = $"{CACHE_KEY_CATEGORY}{category.ToLowerInvariant()}";

            return await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    _logger.LogDebug("Cache MISS for category: {Category}", category);
                    return await _innerRepository.GetByCategoryAsync(category);
                },
                options => ConfigureEntryOptions(options, "GetByCategory", TimeSpan.FromMinutes(7))
            );
        }

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            var cacheKey = $"{CACHE_KEY_SEARCH}{searchTerm.ToLowerInvariant()}";

            return await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    _logger.LogDebug("Cache MISS for search: {SearchTerm}", searchTerm);
                    return await _innerRepository.SearchAsync(searchTerm);
                },
                options => ConfigureEntryOptions(options, "Search", TimeSpan.FromMinutes(3))
            );
        }

        public async Task<long> GetCountAsync()
        {
            return await _cache.GetOrSetAsync(
                CACHE_KEY_COUNT,
                async ct =>
                {
                    _logger.LogDebug("Cache MISS for count");
                    return await _innerRepository.GetCountAsync();
                },
                options => ConfigureEntryOptions(options, "GetCount", TimeSpan.FromMinutes(2))
            );
        }

        public async Task<Product> CreateAsync(Product product)
        {
            var result = await _innerRepository.CreateAsync(product);

            // Invalidate related caches
            await InvalidateRelatedCachesAsync(result, "Create");

            _logger.LogInformation("Product created and caches invalidated: {ProductId}", result.Id);
            return result;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var result = await _innerRepository.UpdateAsync(product);

            if (result)
            {
                // Invalidate specific product cache
                var cacheKey = $"{CACHE_KEY_PREFIX}{product.Id}";
                await _cache.RemoveAsync(cacheKey);

                // Invalidate related caches
                await InvalidateRelatedCachesAsync(product, "Update");

                _logger.LogInformation("Product updated and caches invalidated: {ProductId}", product.Id);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            // Get product first to know which caches to invalidate
            var product = await _innerRepository.GetByIdAsync(id);

            var result = await _innerRepository.DeleteAsync(id);

            if (result && product != null)
            {
                // Invalidate specific product cache
                var cacheKey = $"{CACHE_KEY_PREFIX}{id}";
                await _cache.RemoveAsync(cacheKey);

                // Invalidate related caches
                await InvalidateRelatedCachesAsync(product, "Delete");

                _logger.LogInformation("Product deleted and caches invalidated: {ProductId}", id);
            }

            return result;
        }

        /// <summary>
        /// Configure FusionCache entry options with best practices
        /// </summary>
        private FusionCacheEntryOptions ConfigureEntryOptions(
            FusionCacheEntryOptions options,
            string operationName,
            TimeSpan? customDuration = null)
        {
            options.Duration = customDuration ?? _cacheSettings.DefaultDuration;

            // Fail-Safe: If factory fails, use stale cache for up to 1 hour
            if (_cacheSettings.EnableFailSafe)
            {
                options.IsFailSafeEnabled = true;
                options.FailSafeMaxDuration = _cacheSettings.FailSafeMaxDuration;
                options.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);
            }

            // Soft/Hard timeouts prevent slow databases from blocking
            options.FactorySoftTimeout = _cacheSettings.FactorySoftTimeout;
            options.FactoryHardTimeout = _cacheSettings.FactoryHardTimeout;

            // Eager refresh: Start refreshing before expiration
            options.EagerRefreshThreshold = 0.8f; // Refresh at 80% of duration

            // Enable distributed cache operations
            options.AllowBackgroundDistributedCacheOperations = true;

            // Priority for cache entries
            options.Priority = operationName switch
            {
                "GetById" => CacheItemPriority.High,
                "GetAll" => CacheItemPriority.Normal,
                "GetByCategory" => CacheItemPriority.Normal,
                "Search" => CacheItemPriority.Low,
                "GetCount" => CacheItemPriority.Low,
                _ => CacheItemPriority.Normal
            };

            return options;
        }

        /// <summary>
        /// Invalidate all caches related to a product
        /// Smart invalidation based on product data
        /// </summary>
        private async Task InvalidateRelatedCachesAsync(Product product, string operation)
        {
            // Remove all products list
            await _cache.RemoveAsync(CACHE_KEY_ALL);

            // Remove category cache
            var categoryCacheKey = $"{CACHE_KEY_CATEGORY}{product.Category.ToLowerInvariant()}";
            await _cache.RemoveAsync(categoryCacheKey);

            // Remove count cache
            await _cache.RemoveAsync(CACHE_KEY_COUNT);

            // Note: Search caches are intentionally short-lived (3 min)
            // so we don't need to aggressively invalidate them

            _logger.LogDebug(
                "Invalidated caches for {Operation}: All, Category={Category}, Count",
                operation,
                product.Category);
        }
    }
}
