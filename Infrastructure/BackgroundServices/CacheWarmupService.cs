using Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.BackgroundServices
{
    /// <summary>
    /// Background service to warm up cache on application startup
    /// and periodically refresh hot data
    /// </summary>
    public class CacheWarmupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmupService> _logger;
        private readonly TimeSpan _warmupInterval = TimeSpan.FromMinutes(30);

        public CacheWarmupService(
            IServiceProvider serviceProvider,
            ILogger<CacheWarmupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for application to fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            _logger.LogInformation("Cache Warmup Service started");

            // Initial warmup
            await WarmupCacheAsync();

            // Periodic warmup
            using var timer = new PeriodicTimer(_warmupInterval);

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await WarmupCacheAsync();
            }
        }

        private async Task WarmupCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warmup");

                using var scope = _serviceProvider.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IFusionCache>();
                var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                // Warm up common queries
                _ = await cache.GetOrSetAsync(
                    "products:all",
                    async ct => await productRepo.GetAllAsync(),
                    TimeSpan.FromMinutes(5));

                _ = await cache.GetOrSetAsync(
                    "products:count",
                    async ct => await productRepo.GetCountAsync(),
                    TimeSpan.FromMinutes(2));

                // Warm up popular categories
                var popularCategories = new[] { "Electronics", "Books", "Clothing" };

                foreach (var category in popularCategories)
                {
                    var cacheKey = $"products:category:{category.ToLowerInvariant()}";

                    _ = await cache.GetOrSetAsync(
                        cacheKey,
                        async ct => await productRepo.GetByCategoryAsync(category),
                        TimeSpan.FromMinutes(7));
                }

                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
            }
        }
    }
}
