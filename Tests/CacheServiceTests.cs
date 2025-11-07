using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class CacheServiceTests
    {
        [Fact]
        public async Task GetAsync_WhenKeyExists_ReturnsValue()
        {
            // Arrange
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            var mockLogger = new Mock<ILogger<RedisCacheService>>();

            mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            var testData = "{\"Id\":1,\"Name\":\"Test\"}";
            mockDatabase.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue(testData));

            var cacheService = new RedisCacheService(mockMultiplexer.Object, mockLogger.Object);

            // Act
            var result = await cacheService.GetAsync<TestObject>("test-key");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task SetAsync_Success_ReturnsTrue()
        {
            // Arrange
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            var mockLogger = new Mock<ILogger<RedisCacheService>>();

            mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            mockDatabase.Setup(x => x.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            var cacheService = new RedisCacheService(mockMultiplexer.Object, mockLogger.Object);

            // Act
            var result = await cacheService.SetAsync("test-key", new TestObject { Id = 1 });

            // Assert
            Assert.True(result);
        }

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
