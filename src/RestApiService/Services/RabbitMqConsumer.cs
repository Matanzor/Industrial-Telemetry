using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestApiService.Hubs;
using RestApiService.Models;

namespace RestApiService.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "telemetry.events";
    private const string QueueName = "rest-api-service.telemetry";

    public RabbitMqConsumer(
        IHubContext<TelemetryHub> hubContext,
        IConfiguration config,
        ILogger<RabbitMqConsumer> logger)
    {
        _hubContext = hubContext;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("Connected to RabbitMQ");
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ, retrying in {Delay}s...", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
            }
        }

        if (stoppingToken.IsCancellationRequested) return;

        await _channel!.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, durable: true);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(QueueName, ExchangeName, routingKey: string.Empty);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var reading = JsonSerializer.Deserialize<SensorReadingResponse>(json);

                if (reading != null)
                {
                    // Push to all connected clients
                    await _hubContext.Clients.Group("all-sensors")
                        .SendAsync("ReceiveTelemetry", reading, stoppingToken);

                    // Push to sensor-specific subscribers
                    await _hubContext.Clients.Group($"sensor-{reading.SensorId}")
                        .SendAsync("ReceiveSensorTelemetry", reading, stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry for SignalR broadcast");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("RabbitMQ → SignalR consumer started on queue {Queue}", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
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
        await base.StopAsync(cancellationToken);
    }
}
