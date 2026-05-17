using IoTTelemetryService.Sensors;
using IoTTelemetryService.Services;

namespace IoTTelemetryService;

public class TelemetryWorker : BackgroundService
{
    private readonly ISensorSimulator _simulator;
    private readonly IRedisPublisher _redisPublisher;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<TelemetryWorker> _logger;

    public TelemetryWorker(
        ISensorSimulator simulator,
        IRedisPublisher redisPublisher,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<TelemetryWorker> logger)
    {
        _simulator = simulator;
        _redisPublisher = redisPublisher;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing RabbitMQ publisher...");
        await _rabbitMqPublisher.InitializeAsync();
        _logger.LogInformation("Telemetry worker started. Simulating {Count} sensors.", _simulator.SensorConfigs.Count);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var tasks = new List<Task>();

            foreach (var config in _simulator.SensorConfigs)
            {
                var reading = _simulator.GenerateReading(config);
                tasks.Add(PublishReadingAsync(reading));
            }

            await Task.WhenAll(tasks);
            _logger.LogDebug("Published telemetry for all {Count} sensors", _simulator.SensorConfigs.Count);
        }
    }

    private async Task PublishReadingAsync(Models.SensorReading reading)
    {
        try
        {
            await Task.WhenAll(
                _redisPublisher.PublishAsync(reading),
                _rabbitMqPublisher.PublishAsync(reading)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish telemetry for sensor {SensorId}", reading.SensorId);
        }
    }
}
