using Domain.DistributedLockService;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DistributedLockService : IDistributedLockService
    {
        private readonly IDatabase _database;
        private readonly ILogger<DistributedLockService> _logger;
        private const string LOCK_PREFIX = "lock:";

        public DistributedLockService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<DistributedLockService> logger)
        {
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
        {
            var lockKey = $"{LOCK_PREFIX}{key}";
            var lockValue = Guid.NewGuid().ToString();

            var acquired = await _database.StringSetAsync(
                lockKey,
                lockValue,
                expiry,
                When.NotExists);

            if (acquired)
            {
                _logger.LogDebug("Lock acquired for key: {Key}", key);
            }

            return acquired;
        }

        public async Task<bool> ReleaseLockAsync(string key)
        {
            var lockKey = $"{LOCK_PREFIX}{key}";
            var released = await _database.KeyDeleteAsync(lockKey);

            if (released)
            {
                _logger.LogDebug("Lock released for key: {Key}", key);
            }

            return released;
        }
    }
}
