using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Configuration;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Tests
{
    public class FusionCacheTests
    {
        [Fact]
        public async Task CachedRepository_FirstCall_MissesCache()
        {
            // Arrange
            var mockRepo = new Mock<IProductRepository>();
            var product = new Product { Id = "123", Name = "Test" };

            mockRepo.Setup(x => x.GetByIdAsync("123"))
                .ReturnsAsync(product);

            var cache = new FusionCache(new FusionCacheOptions());
            var logger = new Mock<ILogger<CachedProductRepository>>();
            var settings = Options.Create(new CacheSettings());

            var cachedRepo = new CachedProductRepository(
                mockRepo.Object,
                cache,
                logger.Object,
                settings);

            // Act
            var result = await cachedRepo.GetByIdAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            mockRepo.Verify(x => x.GetByIdAsync("123"), Times.Once);
        }

        [Fact]
        public async Task CachedRepository_SecondCall_HitsCache()
        {
            // Arrange
            var mockRepo = new Mock<IProductRepository>();
            var product = new Product { Id = "123", Name = "Test" };

            mockRepo.Setup(x => x.GetByIdAsync("123"))
                .ReturnsAsync(product);

            var cache = new FusionCache(new FusionCacheOptions());
            var logger = new Mock<ILogger<CachedProductRepository>>();
            var settings = Options.Create(new CacheSettings());

            var cachedRepo = new CachedProductRepository(
                mockRepo.Object,
                cache,
                logger.Object,
                settings);

            // Act
            await cachedRepo.GetByIdAsync("123"); // First call
            var result = await cachedRepo.GetByIdAsync("123"); // Second call

            // Assert
            Assert.NotNull(result);
            mockRepo.Verify(x => x.GetByIdAsync("123"), Times.Once); // Only called once!
        }
    }
}
