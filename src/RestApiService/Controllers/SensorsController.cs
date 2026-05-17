using Microsoft.AspNetCore.Mvc;
using RestApiService.Models;
using RestApiService.Services;

namespace RestApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly IGrpcTelemetryClient _grpcClient;
    private readonly ILogger<SensorsController> _logger;

    public SensorsController(IGrpcTelemetryClient grpcClient, ILogger<SensorsController> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    /// <summary>
    /// Get latest readings for all sensors.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SensorReadingResponse>>> GetAllSensors()
    {
        var sensors = await _grpcClient.GetAllSensorsAsync();
        return Ok(sensors);
    }

    /// <summary>
    /// Get latest reading for a specific sensor.
    /// </summary>
    [HttpGet("{sensorId:int}")]
    public async Task<ActionResult<SensorReadingResponse>> GetSensorById(int sensorId)
    {
        if (sensorId < 1 || sensorId > 20)
            return BadRequest("Sensor ID must be between 1 and 20.");

        var sensor = await _grpcClient.GetSensorByIdAsync(sensorId);
        if (sensor == null)
            return NotFound($"Sensor {sensorId} not found.");

        return Ok(sensor);
    }

    /// <summary>
    /// Get historical readings for a sensor with pagination.
    /// </summary>
    [HttpGet("{sensorId:int}/history")]
    public async Task<ActionResult<SensorHistoryResponse>> GetSensorHistory(
        int sensorId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        if (sensorId < 1 || sensorId > 20)
            return BadRequest("Sensor ID must be between 1 and 20.");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;

        var history = await _grpcClient.GetSensorHistoryAsync(sensorId, from, to, page, pageSize);
        return Ok(history);
    }
}
