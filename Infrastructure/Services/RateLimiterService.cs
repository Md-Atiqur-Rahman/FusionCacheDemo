using Domain.RateLimiterService;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class RateLimiterService : IRateLimiterService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RateLimiterService> _logger;
        private const string RATE_LIMIT_PREFIX = "ratelimit:";

        public RateLimiterService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RateLimiterService> logger)
        {
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> IsAllowedAsync(string identifier, int maxRequests, TimeSpan window)
        {
            var key = $"{RATE_LIMIT_PREFIX}{identifier}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (long)window.TotalMilliseconds;

            var transaction = _database.CreateTransaction();

            // Remove old entries
            var removeTask = transaction.SortedSetRemoveRangeByScoreAsync(
                key,
                0,
                windowStart);

            // Add current request
            var addTask = transaction.SortedSetAddAsync(key, now, now);

            // Get count
            var countTask = transaction.SortedSetLengthAsync(key);

            // Set expiry
            var expireTask = transaction.KeyExpireAsync(key, window);

            var executed = await transaction.ExecuteAsync();

            if (!executed)
            {
                _logger.LogWarning("Rate limit transaction failed for: {Identifier}", identifier);
                return true; // Fail open
            }

            var count = await countTask;
            var allowed = count <= maxRequests;

            if (!allowed)
            {
                _logger.LogWarning("Rate limit exceeded for: {Identifier}, Count: {Count}",
                    identifier, count);
            }

            return allowed;
        }
    }
}
