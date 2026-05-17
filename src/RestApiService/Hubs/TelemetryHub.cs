using Microsoft.AspNetCore.SignalR;

namespace RestApiService.Hubs;

public class TelemetryHub : Hub
{
    private readonly ILogger<TelemetryHub> _logger;

    public TelemetryHub(ILogger<TelemetryHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // All clients automatically join "all-sensors" group
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-sensors");
        _logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public async Task SubscribeToSensor(int sensorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sensor-{sensorId}");
        _logger.LogDebug("Client {ConnectionId} subscribed to sensor {SensorId}", Context.ConnectionId, sensorId);
    }

    public async Task UnsubscribeFromSensor(int sensorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sensor-{sensorId}");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from sensor {SensorId}", Context.ConnectionId, sensorId);
    }
}
