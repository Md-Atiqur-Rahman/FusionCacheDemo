using Domain.CachingService;
using Domain.Repositories;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services.CachingService;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add MongoDB with best practices
        /// </summary>
        public static IServiceCollection AddMongoDb(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(
                configuration.GetSection("MongoDb"));

            services.AddSingleton<MongoDbContext>();

            return services;
        }

        /// <summary>
        /// Add FusionCache with L1 (Memory) + L2 (Redis) + Backplane
        /// FIXED FOR .NET 9 & FusionCache 1.3.0
        /// </summary>
        public static IServiceCollection AddFusionCacheWithRedis(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure cache settings
            services.Configure<CacheSettings>(
                configuration.GetSection("Cache"));

            var cacheSettings = configuration
                .GetSection("Cache")
                .Get<CacheSettings>() ?? new CacheSettings();

            // Get Redis connection string
            var redisConnection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection not found");

            // Configure Redis with retry policy
            var redisConfig = ConfigurationOptions.Parse(redisConnection);
            redisConfig.AbortOnConnectFail = false;
            redisConfig.ConnectRetry = 3;
            redisConfig.ConnectTimeout = 5000;
            redisConfig.SyncTimeout = 5000;

            // Register Redis connection multiplexer as singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConfig));

            // Add Redis as distributed cache (L2) - REQUIRED for FusionCache
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisConfig;
                options.InstanceName = "FusionCacheDemo:";
            });

            // ✅ FIXED: FusionCache 1.3.0+ uses new extension methods
            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = cacheSettings.DefaultDuration;
                    options.IsFailSafeEnabled = cacheSettings.EnableFailSafe;
                    options.FailSafeMaxDuration = cacheSettings.FailSafeMaxDuration;
                    options.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);
                    options.FactorySoftTimeout = cacheSettings.FactorySoftTimeout;
                    options.FactoryHardTimeout = cacheSettings.FactoryHardTimeout;
                    options.AllowBackgroundDistributedCacheOperations = true;
                })
                .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>());


            // ✅ FIXED: Serializer registration (new method name)
            services.AddFusionCacheSystemTextJsonSerializer();

            // ✅ FIXED: Backplane registration with new API
            if (cacheSettings.EnableBackplane)
            {
                services.AddFusionCacheStackExchangeRedisBackplane(options =>
                {
                    options.Configuration = redisConnection;
                });
            }

            return services;
        }

        /// <summary>
        /// Add FusionCache with ONLY L1 (Memory) - Simpler setup
        /// Good for single-server applications
        /// </summary>
        public static IServiceCollection AddFusionCacheMemoryOnly(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<CacheSettings>(
                configuration.GetSection("Cache"));

            var cacheSettings = configuration
                .GetSection("Cache")
                .Get<CacheSettings>() ?? new CacheSettings();

            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = cacheSettings.DefaultDuration;
                    options.IsFailSafeEnabled = cacheSettings.EnableFailSafe;
                    options.FailSafeMaxDuration = cacheSettings.FailSafeMaxDuration;
                });

            return services;
        }

        /// <summary>
        /// Register repositories with decorator pattern
        /// </summary>
        public static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<ProductRepository>();

            services.AddScoped<IProductRepository>(provider =>
            {
                var innerRepository = provider.GetRequiredService<ProductRepository>();
                var cache = provider.GetRequiredService<IFusionCache>();
                var logger = provider.GetRequiredService<ILogger<CachedProductRepository>>();
                var cacheSettings = provider.GetRequiredService<IOptions<CacheSettings>>();

                return new CachedProductRepository(
                    innerRepository,
                    cache,
                    logger,
                    cacheSettings);
            });

            return services;
        }

        /// <summary>
        /// Register application services
        /// </summary>
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

            return services;
        }
    }
}
