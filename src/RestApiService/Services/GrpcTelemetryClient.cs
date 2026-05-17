using Shared.Grpc;
using RestApiService.Models;

namespace RestApiService.Services;

public interface IGrpcTelemetryClient
{
    Task<List<SensorReadingResponse>> GetAllSensorsAsync();
    Task<SensorReadingResponse?> GetSensorByIdAsync(int sensorId);
    Task<SensorHistoryResponse> GetSensorHistoryAsync(int sensorId, string? from, string? to, int page, int pageSize);
}

public class GrpcTelemetryClient : IGrpcTelemetryClient
{
    private readonly TelemetryService.TelemetryServiceClient _client;
    private readonly ILogger<GrpcTelemetryClient> _logger;

    public GrpcTelemetryClient(TelemetryService.TelemetryServiceClient client, ILogger<GrpcTelemetryClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<SensorReadingResponse>> GetAllSensorsAsync()
    {
        var response = await _client.GetAllSensorsAsync(new GetAllSensorsRequest());
        return response.Sensors.Select(MapToResponse).ToList();
    }

    public async Task<SensorReadingResponse?> GetSensorByIdAsync(int sensorId)
    {
        try
        {
            var response = await _client.GetSensorByIdAsync(new GetSensorByIdRequest { SensorId = sensorId });
            return MapToResponse(response);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<SensorHistoryResponse> GetSensorHistoryAsync(
        int sensorId, string? from, string? to, int page, int pageSize)
    {
        var request = new GetSensorHistoryRequest
        {
            SensorId = sensorId,
            FromTimestamp = from ?? string.Empty,
            ToTimestamp = to ?? string.Empty,
            Page = page,
            PageSize = pageSize
        };

        var response = await _client.GetSensorHistoryAsync(request);
        return new SensorHistoryResponse
        {
            Readings = response.Readings.Select(MapToResponse).ToList(),
            TotalCount = response.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static SensorReadingResponse MapToResponse(SensorReadingDto dto) => new()
    {
        SensorId = dto.SensorId,
        SensorType = dto.SensorType,
        Value = dto.Value,
        Unit = dto.Unit,
        Timestamp = dto.Timestamp,
        Status = dto.Status
    };
}
