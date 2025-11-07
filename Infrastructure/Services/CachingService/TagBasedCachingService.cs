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
    public class TagBasedCachingService : ITagBasedCachingService
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<TagBasedCachingService> _logger;

        // Track cache keys by tags (in production, use Redis Sets or similar)
        private readonly Dictionary<string, HashSet<string>> _tagToKeys = new();

        public TagBasedCachingService(
            IFusionCache cache,
            ILogger<TagBasedCachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetUserDataAsync(string userId, string[] tags)
        {
            var cacheKey = $"user:{userId}";

            // Track this key under all tags
            foreach (var tag in tags)
            {
                if (!_tagToKeys.ContainsKey(tag))
                {
                    _tagToKeys[tag] = new HashSet<string>();
                }
                _tagToKeys[tag].Add(cacheKey);
            }

            return await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    _logger.LogInformation("Loading user {UserId} with tags: {Tags}",
                        userId, string.Join(", ", tags));

                    await Task.Delay(100);
                    return $"User {userId} data";
                },
                TimeSpan.FromMinutes(10));
        }

        public async Task InvalidateByTagAsync(string tag)
        {
            if (!_tagToKeys.TryGetValue(tag, out var keys))
            {
                _logger.LogWarning("No cache entries found for tag: {Tag}", tag);
                return;
            }

            _logger.LogInformation(
                "Invalidating {Count} cache entries for tag: {Tag}",
                keys.Count,
                tag);

            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }

            _tagToKeys.Remove(tag);
        }
    }
}
