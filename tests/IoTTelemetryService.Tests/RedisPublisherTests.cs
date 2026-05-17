using FluentAssertions;
using IoTTelemetryService.Models;
using IoTTelemetryService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace IoTTelemetryService.Tests;

public class RedisPublisherTests
{
    [Fact]
    public async Task PublishAsync_ShouldCallRedisDatabase()
    {
        // Arrange
        var mockDb = new Mock<IDatabase>();
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        var logger = new Mock<ILogger<RedisPublisher>>();
        var publisher = new RedisPublisher(mockRedis.Object, logger.Object);

        var reading = new SensorReading
        {
            SensorId = 1,
            SensorType = "Temperature",
            Value = 55.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow,
            Status = "Normal"
        };

        // Act
        await publisher.PublishAsync(reading);

        // Assert - verify GetDatabase was called (meaning we interacted with Redis)
        mockRedis.Verify(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()), Times.Once);
    }
}
