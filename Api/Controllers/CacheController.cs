using Domain.CachingService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Controllers
{
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
            return Ok(new
            {
                message = "FusionCache 1.3.0 running on .NET 9",
                features = new[]
                {
                    "L1 (Memory) Cache",
                    "L2 (Redis) Cache",
                    "Backplane Support",
                    "Fail-Safe Enabled",
                    "Auto-Recovery",
                    "Stampede Protection"
                },
                tip = "Use OpenTelemetry integration for production metrics"
            });
        }

        /// <summary>
        /// Check if a specific key exists in cache
        /// ✅ FIXED: Removed .IsStale (not available in 1.3.0)
        /// </summary>
        [HttpGet("exists/{key}")]
        public async Task<ActionResult> CheckKey(string key)
        {
            var result = await _cache.TryGetAsync<object>(key);

            return Ok(new
            {
                key,
                exists = result.HasValue,
                value = result.HasValue ? result.Value : null,
                message = result.HasValue ? "Key exists in cache" : "Key not found"
            });
        }

        /// <summary>
        /// Get detailed information about a cached key
        /// Shows TTL and metadata
        /// </summary>
        [HttpGet("info/{key}")]
        public async Task<ActionResult> GetKeyInfo(string key)
        {
            var result = await _cache.TryGetAsync<object>(key);

            if (!result.HasValue)
            {
                return NotFound(new { message = $"Key '{key}' not found in cache" });
            }

            return Ok(new
            {
                key,
                exists = true,
                hasValue = result.HasValue,
                valueType = result.Value?.GetType().Name ?? "null",
                message = "Cache entry found"
            });
        }

        /// <summary>
        /// Manually set a value in cache (for testing)
        /// </summary>
        [HttpPost("set")]
        public async Task<ActionResult> SetValue(
            [FromQuery] string key,
            [FromQuery] string value,
            [FromQuery] int durationMinutes = 5)
        {
            await _cache.SetAsync(
                key,
                value,
                options => options.Duration = TimeSpan.FromMinutes(durationMinutes));

            _logger.LogInformation("Cache key set: {Key} for {Duration} minutes", key, durationMinutes);

            return Ok(new
            {
                key,
                value,
                durationMinutes,
                message = "Cache entry created successfully"
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
        /// Test fail-safe mechanism
        /// ✅ FIXED: Updated for FusionCache 1.3.0 API
        /// </summary>
        [HttpGet("test-failsafe/{key}")]
        public async Task<ActionResult> TestFailSafe(string key)
        {
            try
            {
                var failSafeDefault = MaybeValue<string>.FromValue("Fallback value");
                var result = await _cache.GetOrSetAsync(
                    key,
                    async ct =>
                    {
                        // Simulate database failure
                        await Task.Delay(100, ct);
                        throw new Exception("Simulated database failure!");
                    },
                    failSafeDefault,
                    options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(10);
                        options.IsFailSafeEnabled = true;
                        options.FailSafeMaxDuration = TimeSpan.FromMinutes(5);
                    });

                return Ok(new
                {
                    success = true,
                    hasValue = result != null,
                    value = result,
                    message = "Fail-safe worked! Returned stale cache instead of error"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "First call - no cache exists yet. Try again!",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Warm up cache with test data
        /// </summary>
        [HttpPost("warmup")]
        public async Task<ActionResult> WarmUp()
        {
            var testKeys = new[] { "test1", "test2", "test3" };

            foreach (var key in testKeys)
            {
                await _cache.SetAsync(
                    key,
                    $"Warmed up value for {key}",
                    TimeSpan.FromMinutes(10));
            }

            _logger.LogInformation("Cache warm-up completed with {Count} entries", testKeys.Length);

            return Ok(new
            {
                message = "Cache warmed up successfully",
                keysCreated = testKeys,
                duration = "10 minutes"
            });
        }
    }
}
