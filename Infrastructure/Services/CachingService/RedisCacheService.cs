using Domain.CachingService;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services.CachingService
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);

                if (!value.HasValue)
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return default;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var result = await _database.StringSetAsync(key, serializedValue, expiration);

                if (result)
                {
                    _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}",
                        key, expiration?.TotalSeconds ?? -1);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                var result = await _database.KeyDeleteAsync(key);

                if (result)
                {
                    _logger.LogDebug("Cache removed for key: {Key}", key);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                // Try to get from cache first
                var cachedValue = await GetAsync<T>(key);

                if (cachedValue != null)
                {
                    return cachedValue;
                }

                // If not in cache, get from factory
                _logger.LogDebug("Cache miss for key: {Key}, fetching from source", key);
                var value = await factory();

                // Store in cache for next time
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSet for key: {Key}", key);
                throw;
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var endpoints = _connectionMultiplexer.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    var server = _connectionMultiplexer.GetServer(endpoint);
                    var keys = server.Keys(pattern: pattern);

                    foreach (var key in keys)
                    {
                        await _database.KeyDeleteAsync(key);
                    }
                }

                _logger.LogDebug("Cache cleared for pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}
