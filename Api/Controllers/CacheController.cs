using Domain.CachingService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Controllers
{
    /// <summary>
    /// Controller for cache management operations
    /// Useful for testing and admin operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly IFusionCache _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            IFusionCache cache,
            ICacheInvalidationService cacheInvalidation,
            ILogger<CacheController> logger)
        {
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("stats")]
        public ActionResult GetStats()
        {
            // Note: FusionCache stats require plugin installation
            // Install: ZiggyCreatures.FusionCache.Plugins.Metrics.Core
            return Ok(new
            {
                message = "Install FusionCache.Plugins.Metrics for detailed stats",
                tip = "Use OpenTelemetry integration for production monitoring"
            });
        }

        /// <summary>
        /// Check if a specific key exists in cache
        /// </summary>
        [HttpGet("exists/{key}")]
        public async Task<ActionResult> CheckKey(string key)
        {
            var result = await _cache.TryGetAsync<object>(key);

            return Ok(new
            {
                key,
                exists = result.HasValue,
                fromMemory = result.HasValue
            });
        }

        /// <summary>
        /// Manually invalidate a specific cache key
        /// </summary>
        [HttpDelete("{key}")]
        public async Task<ActionResult> InvalidateKey(string key)
        {
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Cache key invalidated: {Key}", key);

            return Ok(new { message = $"Cache key '{key}' invalidated" });
        }

        /// <summary>
        /// Invalidate all product-related caches
        /// </summary>
        [HttpDelete("products")]
        public async Task<ActionResult> InvalidateProductCaches([FromQuery] string? category = null)
        {
            await _cacheInvalidation.InvalidateProductCachesAsync(category);

            var message = category != null
                ? $"Product caches invalidated for category: {category}"
                : "All product list caches invalidated";

            _logger.LogInformation(message);
            return Ok(new { message });
        }

        /// <summary>
        /// Warm up cache with frequently accessed data
        /// </summary>
        [HttpPost("warmup")]
        public ActionResult WarmUp()
        {
            // Trigger cache warming logic
            _logger.LogInformation("Cache warm-up initiated");

            return Accepted(new
            {
                message = "Cache warm-up initiated",
                tip = "Implement background service for automatic warming"
            });
        }

        /// <summary>
        /// Test fail-safe mechanism
        /// Simulates database failure to show fail-safe in action
        /// </summary>
        [HttpGet("test-failsafe/{key}")]
        public async Task<ActionResult> TestFailSafe(string key)
        {
            var options = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromSeconds(10),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromMinutes(5)
            };

            // ✅ Fail-safe default value (if factory fails & no previous cache exists)
            var failSafeDefault = MaybeValue<string>.None;

            var result = await _cache.GetOrSetAsync(
                key,
                async (ctx, ct) =>
                {
                    await Task.Delay(100);
                    throw new Exception("Simulated DB failure!");
                },
                failSafeDefault,  // 👈 3rd parameter
                options           // 👈 4th parameter
            );

            return Ok(new
            {
                message = "Fail-safe test",
                hasValue = result != null,
                tip = "First call will fail. Second call returns cached fail-safe value"
            });
        }

    }
}
