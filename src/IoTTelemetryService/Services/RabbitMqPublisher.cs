using System.Text;
using System.Text.Json;
using IoTTelemetryService.Models;
using RabbitMQ.Client;

namespace IoTTelemetryService.Services;

public interface IRabbitMqPublisher : IAsyncDisposable
{
    Task InitializeAsync();
    Task PublishAsync(SensorReading reading);
}

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "telemetry.events";

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"] ?? "localhost",
            Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
            UserName = _config["RabbitMq:Username"] ?? "guest",
            Password = _config["RabbitMq:Password"] ?? "guest"
        };

        // Retry connection with exponential backoff
        var retryDelay = TimeSpan.FromSeconds(2);
        while (true)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                _logger.LogInformation("Connected to RabbitMQ");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ, retrying in {Delay}s...", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay);
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
            }
        }

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ publisher initialized on exchange {Exchange}", ExchangeName);
    }

    public async Task PublishAsync(SensorReading reading)
    {
        if (_channel == null)
            throw new InvalidOperationException("Publisher not initialized. Call InitializeAsync first.");

        var json = JsonSerializer.Serialize(reading);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogDebug("Published telemetry for sensor {SensorId}", reading.SensorId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
