//using Domain.CachingService;
//using Domain.Entities;
//using Domain.Repositories;
//using Infrastructure.Repositories;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Tests
//{
//    public class CachedProductRepositoryTests
//    {
//        [Fact]
//        public async Task GetByIdAsync_CacheHit_DoesNotCallInnerRepository()
//        {
//            // Arrange
//            var mockInnerRepo = new Mock<IProductRepository>();
//            var mockCache = new Mock<ICacheService>();
//            var mockLogger = new Mock<ILogger<CachedProductRepository>>();

//            var cachedProduct = new Product { Id = 1, Name = "Cached Product" };
//            mockCache.Setup(x => x.GetAsync<Product>(It.IsAny<string>()))
//                .ReturnsAsync(cachedProduct);

//            var repository = new CachedProductRepository(
//                mockInnerRepo.Object,
//                mockCache.Object,
//                mockLogger.Object);

//            // Act
//            var result = await repository.GetByIdAsync(1);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal("Cached Product", result.Name);
//            mockInnerRepo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
//        }

//        [Fact]
//        public async Task UpdateAsync_Success_InvalidatesCache()
//        {
//            // Arrange
//            var mockInnerRepo = new Mock<IProductRepository>();
//            var mockCache = new Mock<ICacheService>();
//            var mockLogger = new Mock<ILogger<CachedProductRepository>>();

//            mockInnerRepo.Setup(x => x.UpdateAsync(It.IsAny<Product>()))
//                .ReturnsAsync(true);

//            var repository = new CachedProductRepository(
//                mockInnerRepo.Object,
//                mockCache.Object,
//                mockLogger.Object);

//            // Act
//            await repository.UpdateAsync(new Product { Id = 1 });

//            // Assert
//            mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
//        }
//    }
//}
