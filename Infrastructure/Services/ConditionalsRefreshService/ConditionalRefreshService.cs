using Domain.ConditionalRefreshService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Services.ConditionalsRefreshService
{
    public class ConditionalRefreshService : IConditionalRefreshService
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<ConditionalRefreshService> _logger;

        public ConditionalRefreshService(
            IFusionCache cache,
            ILogger<ConditionalRefreshService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetDataWithConditionalRefreshAsync(string key)
        {
            // Explicitly specify TValue as string
            return await _cache.GetOrSetAsync<string>(
                key,
                async (ctx, ct) =>
                {
                    var now = DateTime.UtcNow;
                    var data = $"Data generated at {now:HH:mm:ss}";

                    // Only allow refresh during business hours (9 AM - 5 PM UTC)
                    var isBusinessHours = now.Hour >= 9 && now.Hour < 17;

                    if (!isBusinessHours && ctx.HasStaleValue)
                    {
                        _logger.LogInformation(
                            "Outside business hours - reusing stale value instead of refreshing");
                        return ctx.StaleValue!;
                    }

                    _logger.LogInformation("Fetching fresh data at {Time}", now);
                    await Task.Delay(500); // Simulate expensive operation

                    return data;
                },
                options =>
                {
                    // Default cache entry options
                    options.Duration = TimeSpan.FromMinutes(5);
                    options.IsFailSafeEnabled = true;
                    options.FailSafeMaxDuration = TimeSpan.FromHours(2);
                    options.FactorySoftTimeout = TimeSpan.FromMilliseconds(500);
                    options.FactoryHardTimeout = TimeSpan.FromSeconds(3);
                    options.AllowBackgroundDistributedCacheOperations = true;
                }
            );
        }

    }
}
