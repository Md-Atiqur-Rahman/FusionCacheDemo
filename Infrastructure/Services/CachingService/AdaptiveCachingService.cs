using Domain.CachingService;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Infrastructure.Services.CachingService
{
    public class AdaptiveCachingService : IAdaptiveCachingService
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<AdaptiveCachingService> _logger;

        public AdaptiveCachingService(
            IFusionCache cache,
            ILogger<AdaptiveCachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<Product?> GetProductWithAdaptiveCachingAsync(string id)
        {
            var cacheKey = $"adaptive:product:{id}";

            return await _cache.GetOrSetAsync<Product>(
                cacheKey,
                async (ctx, ct) =>
                {
                    _logger.LogInformation("Loading product {ProductId} from database", id);
                    await Task.Delay(100);

                    var product = new Product
                    {
                        Id = id,
                        Name = "Sample Product",
                        Stock = 15,
                        Price = 99.99m
                    };

                    // Adaptive duration based on stock
                    if (product.Stock < 10)
                        ctx.Options.Duration = TimeSpan.FromMinutes(2);
                    else if (product.Stock > 100)
                        ctx.Options.Duration = TimeSpan.FromMinutes(30);
                    else
                        ctx.Options.Duration = TimeSpan.FromMinutes(10);

                    ctx.Options.EagerRefreshThreshold = product.Stock < 10 ? 0.5f : 0.8f;

                    return product;
                }
            );

        }
    }
}
