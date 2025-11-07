using Domain.CachingService;
using Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.CachingService
{
    /// <summary>
    /// Background service to pre-warm cache with frequently accessed data
    /// </summary>
    public class CacheWarmerService : BackgroundService
    {
        private readonly ICacheService _cacheService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmerService> _logger;
        private static readonly TimeSpan WarmInterval = TimeSpan.FromMinutes(30);

        public CacheWarmerService(
            ICacheService cacheService,
            IServiceProvider serviceProvider,
            ILogger<CacheWarmerService> logger)
        {
            _cacheService = cacheService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cache Warmer Service started");

            // Initial warm-up
            await WarmCacheAsync();

            // Periodic warm-up
            using var timer = new PeriodicTimer(WarmInterval);

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await WarmCacheAsync();
            }
        }

        private async Task WarmCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warm-up");

                using var scope = _serviceProvider.CreateScope();
                var productRepository = scope.ServiceProvider
                    .GetRequiredService<IProductRepository>();

                // Pre-load frequently accessed data
                var products = await productRepository.GetAllAsync();

                // Cache individual products
                var tasks = products.Select(async product =>
                {
                    var key = $"product:{product.Id}";
                    await _cacheService.SetAsync(key, product, TimeSpan.FromMinutes(10));
                });

                await Task.WhenAll(tasks);

                _logger.LogInformation("Cache warm-up completed. Cached {Count} items",
                    products.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warm-up");
            }
        }
    }
}
