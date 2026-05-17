namespace RestApiService.Models;

public class SensorReadingResponse
{
    public int SensorId { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Status { get; set; } = "Normal";
}

public class SensorHistoryRequest
{
    public string? From { get; set; }
    public string? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

public class SensorHistoryResponse
{
    public List<SensorReadingResponse> Readings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
