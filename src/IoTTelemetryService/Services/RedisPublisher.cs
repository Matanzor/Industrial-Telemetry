using System.Text.Json;
using IoTTelemetryService.Models;
using StackExchange.Redis;

namespace IoTTelemetryService.Services;

public interface IRedisPublisher
{
    Task PublishAsync(SensorReading reading);
}

public class RedisPublisher : IRedisPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPublisher> _logger;

    public RedisPublisher(IConnectionMultiplexer redis, ILogger<RedisPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(SensorReading reading)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(reading);
        var key = $"sensor:{reading.SensorId}";

        await db.StringSetAsync(key, json);
        _logger.LogDebug("Redis SET {Key}", key);
    }
}
