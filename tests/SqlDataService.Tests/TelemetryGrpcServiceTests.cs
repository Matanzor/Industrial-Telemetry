using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;
using SqlDataService.Models;
using SqlDataService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Grpc.Core;

namespace SqlDataService.Tests;

public class TelemetryGrpcServiceTests
{
    private TelemetryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TelemetryDbContext(options);
    }

    private static ServerCallContext CreateMockContext()
    {
        return new MockServerCallContext();
    }

    [Fact]
    public async Task GetAllSensors_ShouldReturnLatestReadingPerSensor()
    {
        // Arrange
        var db = CreateInMemoryContext();
        db.SensorReadings.AddRange(
            new SensorReading { SensorId = 1, SensorType = "Temperature", Value = 50, Unit = "°C", Timestamp = DateTime.UtcNow.AddSeconds(-2), Status = "Normal" },
            new SensorReading { SensorId = 1, SensorType = "Temperature", Value = 55, Unit = "°C", Timestamp = DateTime.UtcNow, Status = "Normal" },
            new SensorReading { SensorId = 2, SensorType = "Pressure", Value = 60, Unit = "PSI", Timestamp = DateTime.UtcNow, Status = "Normal" }
        );
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<TelemetryGrpcService>>();
        var service = new TelemetryGrpcService(db, logger.Object);

        // Act
        var response = await service.GetAllSensors(new Shared.Grpc.GetAllSensorsRequest(), CreateMockContext());

        // Assert
        response.Sensors.Should().HaveCount(2);
        var sensor1 = response.Sensors.First(s => s.SensorId == 1);
        sensor1.Value.Should().Be(55); // latest reading
    }

    [Fact]
    public async Task GetSensorById_ShouldReturnLatestReading()
    {
        var db = CreateInMemoryContext();
        db.SensorReadings.AddRange(
            new SensorReading { SensorId = 5, SensorType = "Pressure", Value = 40, Unit = "PSI", Timestamp = DateTime.UtcNow.AddSeconds(-5), Status = "Normal" },
            new SensorReading { SensorId = 5, SensorType = "Pressure", Value = 65, Unit = "PSI", Timestamp = DateTime.UtcNow, Status = "Warning" }
        );
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<TelemetryGrpcService>>();
        var service = new TelemetryGrpcService(db, logger.Object);

        var response = await service.GetSensorById(
            new Shared.Grpc.GetSensorByIdRequest { SensorId = 5 }, CreateMockContext());

        response.Value.Should().Be(65);
        response.Status.Should().Be("Warning");
    }

    [Fact]
    public async Task GetSensorById_ShouldThrowNotFound_WhenSensorMissing()
    {
        var db = CreateInMemoryContext();
        var logger = new Mock<ILogger<TelemetryGrpcService>>();
        var service = new TelemetryGrpcService(db, logger.Object);

        var act = async () => await service.GetSensorById(
            new Shared.Grpc.GetSensorByIdRequest { SensorId = 999 }, CreateMockContext());

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.NotFound);
    }

    [Fact]
    public async Task GetSensorHistory_ShouldReturnPaginatedResults()
    {
        var db = CreateInMemoryContext();
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 50; i++)
        {
            db.SensorReadings.Add(new SensorReading
            {
                SensorId = 1,
                SensorType = "Temperature",
                Value = 50 + i * 0.1,
                Unit = "°C",
                Timestamp = baseTime.AddSeconds(-i),
                Status = "Normal"
            });
        }
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<TelemetryGrpcService>>();
        var service = new TelemetryGrpcService(db, logger.Object);

        var response = await service.GetSensorHistory(
            new Shared.Grpc.GetSensorHistoryRequest
            {
                SensorId = 1,
                Page = 1,
                PageSize = 10
            }, CreateMockContext());

        response.TotalCount.Should().Be(50);
        response.Readings.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetSensorHistory_ShouldFilterByTimeRange()
    {
        var db = CreateInMemoryContext();
        var baseTime = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        
        db.SensorReadings.AddRange(
            new SensorReading { SensorId = 1, SensorType = "Temperature", Value = 50, Unit = "°C", Timestamp = baseTime.AddHours(-2), Status = "Normal" },
            new SensorReading { SensorId = 1, SensorType = "Temperature", Value = 55, Unit = "°C", Timestamp = baseTime, Status = "Normal" },
            new SensorReading { SensorId = 1, SensorType = "Temperature", Value = 60, Unit = "°C", Timestamp = baseTime.AddHours(2), Status = "Normal" }
        );
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<TelemetryGrpcService>>();
        var service = new TelemetryGrpcService(db, logger.Object);

        var response = await service.GetSensorHistory(
            new Shared.Grpc.GetSensorHistoryRequest
            {
                SensorId = 1,
                FromTimestamp = baseTime.AddHours(-1).ToString("o"),
                ToTimestamp = baseTime.AddHours(1).ToString("o"),
                Page = 1,
                PageSize = 100
            }, CreateMockContext());

        response.TotalCount.Should().Be(1);
        response.Readings.First().Value.Should().Be(55);
    }
}

/// <summary>
/// Minimal mock implementation of ServerCallContext for testing.
/// </summary>
public class MockServerCallContext : ServerCallContext
{
    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "test-peer";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => new();
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => new();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
