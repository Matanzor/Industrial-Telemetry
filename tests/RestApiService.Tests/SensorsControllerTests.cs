using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RestApiService.Controllers;
using RestApiService.Models;
using RestApiService.Services;

namespace RestApiService.Tests;

public class SensorsControllerTests
{
    private readonly Mock<IGrpcTelemetryClient> _mockGrpc;
    private readonly Mock<ILogger<SensorsController>> _mockLogger;
    private readonly SensorsController _controller;

    public SensorsControllerTests()
    {
        _mockGrpc = new Mock<IGrpcTelemetryClient>();
        _mockLogger = new Mock<ILogger<SensorsController>>();
        _controller = new SensorsController(_mockGrpc.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllSensors_ShouldReturnOkWithSensors()
    {
        var sensors = new List<SensorReadingResponse>
        {
            new() { SensorId = 1, SensorType = "Temperature", Value = 55.0, Unit = "°C", Status = "Normal" },
            new() { SensorId = 2, SensorType = "Pressure", Value = 60.0, Unit = "PSI", Status = "Normal" }
        };
        _mockGrpc.Setup(g => g.GetAllSensorsAsync()).ReturnsAsync(sensors);

        var result = await _controller.GetAllSensors();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<SensorReadingResponse>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSensorById_ShouldReturnOk_WhenSensorExists()
    {
        var sensor = new SensorReadingResponse
        {
            SensorId = 1, SensorType = "Temperature", Value = 55.0, Unit = "°C", Status = "Normal"
        };
        _mockGrpc.Setup(g => g.GetSensorByIdAsync(1)).ReturnsAsync(sensor);

        var result = await _controller.GetSensorById(1);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<SensorReadingResponse>().Subject;
        data.SensorId.Should().Be(1);
    }

    [Fact]
    public async Task GetSensorById_ShouldReturnNotFound_WhenSensorMissing()
    {
        _mockGrpc.Setup(g => g.GetSensorByIdAsync(1)).ReturnsAsync((SensorReadingResponse?)null);

        var result = await _controller.GetSensorById(1);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public async Task GetSensorById_ShouldReturnBadRequest_ForInvalidId(int sensorId)
    {
        var result = await _controller.GetSensorById(sensorId);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSensorHistory_ShouldReturnOk()
    {
        var history = new SensorHistoryResponse
        {
            Readings = new List<SensorReadingResponse>
            {
                new() { SensorId = 1, Value = 50, Unit = "°C" },
                new() { SensorId = 1, Value = 55, Unit = "°C" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 100
        };
        _mockGrpc.Setup(g => g.GetSensorHistoryAsync(1, null, null, 1, 100)).ReturnsAsync(history);

        var result = await _controller.GetSensorHistory(1, null, null, 1, 100);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<SensorHistoryResponse>().Subject;
        data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSensorHistory_ShouldReturnBadRequest_ForInvalidId()
    {
        var result = await _controller.GetSensorHistory(0, null, null, 1, 100);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
