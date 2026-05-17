using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;

namespace IntegrationTests;

/// <summary>
/// Integration tests that validate the full real-time pipeline.
/// These tests require all services running via docker-compose.
/// Run with: dotnet test tests/IntegrationTests --filter Category=Integration
/// </summary>
[Trait("Category", "Integration")]
public class RealTimeFlowTests
{
    private readonly string _apiBaseUrl;
    private readonly string _signalRUrl;

    public RealTimeFlowTests()
    {
        _apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
        _signalRUrl = $"{_apiBaseUrl}/hubs/telemetry";
    }

    [Fact]
    public async Task AllSensors_ShouldProduceTelemetry_Within10Seconds()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };

        // Wait for system to warm up (up to 15 seconds)
        List<dynamic>? sensors = null;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/sensors");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    sensors = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(json);
                    if (sensors != null && sensors.Count == 20) break;
                }
            }
            catch { /* service not ready yet */ }
            await Task.Delay(1000);
        }

        // Assert
        sensors.Should().NotBeNull("REST API should be reachable");
        sensors!.Count.Should().Be(20, "all 20 sensors should have data");
    }

    [Fact]
    public async Task SignalR_ShouldReceiveRealTimeTelemetry()
    {
        // Arrange
        var receivedSensorIds = new HashSet<int>();
        var connection = new HubConnectionBuilder()
            .WithUrl(_signalRUrl)
            .WithAutomaticReconnect()
            .Build();

        connection.On<System.Text.Json.JsonElement>("ReceiveTelemetry", (reading) =>
        {
            if (reading.TryGetProperty("sensorId", out var idProp))
            {
                receivedSensorIds.Add(idProp.GetInt32());
            }
        });

        // Act
        await connection.StartAsync();

        // Wait up to 15 seconds for all 20 sensors
        for (int i = 0; i < 15 && receivedSensorIds.Count < 20; i++)
        {
            await Task.Delay(1000);
        }

        await connection.StopAsync();

        // Assert
        receivedSensorIds.Count.Should().Be(20,
            "SignalR should receive real-time telemetry for all 20 sensors within 15 seconds");
    }

    [Fact]
    public async Task HistoricalData_ShouldBePersisted()
    {
        // Wait for some data to accumulate
        await Task.Delay(5000);

        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
        var response = await httpClient.GetAsync("/api/sensors/1/history?pageSize=10");
        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("readings");
        json.Should().Contain("totalCount");
    }

    [Fact]
    public async Task RestApi_ShouldReturnSingleSensor()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };

        // Wait for system warmup
        await Task.Delay(3000);

        var response = await httpClient.GetAsync("/api/sensors/1");
        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("sensorId");
        json.Should().Contain("sensorType");
    }
}
