using IoTTelemetryService;
using IoTTelemetryService.Sensors;
using IoTTelemetryService.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Redis (with retry)
var redisConnStr = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
    var retryDelay = TimeSpan.FromSeconds(2);
    while (true)
    {
        try
        {
            var mux = ConnectionMultiplexer.Connect(redisConnStr);
            logger.LogInformation("Connected to Redis");
            return mux;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to Redis, retrying in {Delay}s...", retryDelay.TotalSeconds);
            Thread.Sleep(retryDelay);
            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
        }
    }
});

// Services
builder.Services.AddSingleton<ISensorSimulator, SensorSimulator>();
builder.Services.AddSingleton<IRedisPublisher, RedisPublisher>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<TelemetryWorker>();

var host = builder.Build();
host.Run();
