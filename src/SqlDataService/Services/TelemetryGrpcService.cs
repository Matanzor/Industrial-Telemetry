using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Grpc;
using SqlDataService.Data;

namespace SqlDataService.Services;

public class TelemetryGrpcService : TelemetryService.TelemetryServiceBase
{
    private readonly TelemetryDbContext _db;
    private readonly ILogger<TelemetryGrpcService> _logger;

    public TelemetryGrpcService(TelemetryDbContext db, ILogger<TelemetryGrpcService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task<GetAllSensorsResponse> GetAllSensors(
        GetAllSensorsRequest request, ServerCallContext context)
    {
        // Get latest reading per sensor
        var latestReadings = await _db.SensorReadings
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToListAsync(context.CancellationToken);

        var response = new GetAllSensorsResponse();
        foreach (var r in latestReadings)
        {
            response.Sensors.Add(MapToDto(r));
        }
        return response;
    }

    public override async Task<SensorReadingDto> GetSensorById(
        GetSensorByIdRequest request, ServerCallContext context)
    {
        var reading = await _db.SensorReadings
            .Where(r => r.SensorId == request.SensorId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (reading == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Sensor {request.SensorId} not found"));

        return MapToDto(reading);
    }

    public override async Task<GetSensorHistoryResponse> GetSensorHistory(
        GetSensorHistoryRequest request, ServerCallContext context)
    {
        var query = _db.SensorReadings
            .Where(r => r.SensorId == request.SensorId);

        if (!string.IsNullOrEmpty(request.FromTimestamp) &&
            DateTime.TryParse(request.FromTimestamp, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var from))
        {
            query = query.Where(r => r.Timestamp >= from);
        }

        if (!string.IsNullOrEmpty(request.ToTimestamp) &&
            DateTime.TryParse(request.ToTimestamp, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var to))
        {
            query = query.Where(r => r.Timestamp <= to);
        }

        var totalCount = await query.CountAsync(context.CancellationToken);

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 100;
        pageSize = Math.Min(pageSize, 1000); // cap at 1000

        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new GetSensorHistoryResponse { TotalCount = totalCount };
        foreach (var r in readings)
        {
            response.Readings.Add(MapToDto(r));
        }
        return response;
    }

    private static SensorReadingDto MapToDto(Models.SensorReading r) => new()
    {
        SensorId = r.SensorId,
        SensorType = r.SensorType,
        Value = r.Value,
        Unit = r.Unit,
        Timestamp = r.Timestamp.ToString("o"),
        Status = r.Status
    };
}
