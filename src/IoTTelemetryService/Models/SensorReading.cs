namespace IoTTelemetryService.Models;

public class SensorReading
{
    public int SensorId { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "Normal";
}

public class SensorConfig
{
    public int SensorId { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double NominalValue { get; set; }
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
}
