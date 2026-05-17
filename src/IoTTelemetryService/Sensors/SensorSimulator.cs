using IoTTelemetryService.Models;

namespace IoTTelemetryService.Sensors;

public interface ISensorSimulator
{
    IReadOnlyList<SensorConfig> SensorConfigs { get; }
    SensorReading GenerateReading(SensorConfig config);
}

public class SensorSimulator : ISensorSimulator
{
    private readonly Random _random = new();
    private readonly Dictionary<int, double> _lastValues = new();

    public IReadOnlyList<SensorConfig> SensorConfigs { get; }

    public SensorSimulator()
    {
        SensorConfigs = BuildSensorConfigs();
    }

    public SensorReading GenerateReading(SensorConfig config)
    {
        if (!_lastValues.TryGetValue(config.SensorId, out var lastValue))
        {
            lastValue = config.NominalValue;
        }

        // Mean-reverting random walk: drifts toward nominal with noise
        var drift = (config.NominalValue - lastValue) * 0.1;
        var noise = (_random.NextDouble() - 0.5) * (config.MaxValue - config.MinValue) * 0.05;
        var newValue = lastValue + drift + noise;
        newValue = Math.Clamp(newValue, config.MinValue, config.MaxValue);
        newValue = Math.Round(newValue, 2);

        _lastValues[config.SensorId] = newValue;

        var deviation = Math.Abs(newValue - config.NominalValue);
        var status = deviation >= config.CriticalThreshold ? "Critical"
                   : deviation >= config.WarningThreshold ? "Warning"
                   : "Normal";

        return new SensorReading
        {
            SensorId = config.SensorId,
            SensorType = config.SensorType,
            Value = newValue,
            Unit = config.Unit,
            Timestamp = DateTime.UtcNow,
            Status = status
        };
    }

    private static List<SensorConfig> BuildSensorConfigs()
    {
        var configs = new List<SensorConfig>();

        // 4 sensors per type × 5 types = 20 sensors
        for (int i = 1; i <= 4; i++)
        {
            configs.Add(new SensorConfig
            {
                SensorId = i,
                SensorType = "Temperature",
                Unit = "°C",
                MinValue = 15, MaxValue = 95,
                NominalValue = 55,
                WarningThreshold = 20,
                CriticalThreshold = 35
            });
        }
        for (int i = 5; i <= 8; i++)
        {
            configs.Add(new SensorConfig
            {
                SensorId = i,
                SensorType = "Pressure",
                Unit = "PSI",
                MinValue = 10, MaxValue = 120,
                NominalValue = 60,
                WarningThreshold = 25,
                CriticalThreshold = 45
            });
        }
        for (int i = 9; i <= 12; i++)
        {
            configs.Add(new SensorConfig
            {
                SensorId = i,
                SensorType = "Humidity",
                Unit = "%",
                MinValue = 10, MaxValue = 90,
                NominalValue = 45,
                WarningThreshold = 20,
                CriticalThreshold = 35
            });
        }
        for (int i = 13; i <= 16; i++)
        {
            configs.Add(new SensorConfig
            {
                SensorId = i,
                SensorType = "Vibration",
                Unit = "mm/s",
                MinValue = 0, MaxValue = 50,
                NominalValue = 10,
                WarningThreshold = 15,
                CriticalThreshold = 25
            });
        }
        for (int i = 17; i <= 20; i++)
        {
            configs.Add(new SensorConfig
            {
                SensorId = i,
                SensorType = "Speed",
                Unit = "RPM",
                MinValue = 500, MaxValue = 5000,
                NominalValue = 2500,
                WarningThreshold = 1000,
                CriticalThreshold = 1800
            });
        }

        return configs;
    }
}
