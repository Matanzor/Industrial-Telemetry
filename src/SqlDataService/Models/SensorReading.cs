namespace SqlDataService.Models;

public class SensorReading
{
    public long Id { get; set; }
    public int SensorId { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Normal";
}
