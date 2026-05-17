using FluentAssertions;
using IoTTelemetryService.Models;
using IoTTelemetryService.Sensors;

namespace IoTTelemetryService.Tests;

public class SensorSimulatorTests
{
    private readonly SensorSimulator _simulator = new();

    [Fact]
    public void ShouldHaveExactly20SensorConfigs()
    {
        _simulator.SensorConfigs.Should().HaveCount(20);
    }

    [Fact]
    public void ShouldHave4SensorsPerType()
    {
        var grouped = _simulator.SensorConfigs.GroupBy(c => c.SensorType);
        foreach (var group in grouped)
        {
            group.Should().HaveCount(4, $"expected 4 sensors of type {group.Key}");
        }
    }

    [Fact]
    public void ShouldHave5SensorTypes()
    {
        var types = _simulator.SensorConfigs.Select(c => c.SensorType).Distinct().ToList();
        types.Should().HaveCount(5);
        types.Should().Contain(new[] { "Temperature", "Pressure", "Humidity", "Vibration", "Speed" });
    }

    [Fact]
    public void ShouldHaveUniqueSensorIds()
    {
        var ids = _simulator.SensorConfigs.Select(c => c.SensorId).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().BeEquivalentTo(Enumerable.Range(1, 20));
    }

    [Fact]
    public void GenerateReading_ShouldReturnValidReading()
    {
        var config = _simulator.SensorConfigs[0];
        var reading = _simulator.GenerateReading(config);

        reading.SensorId.Should().Be(config.SensorId);
        reading.SensorType.Should().Be(config.SensorType);
        reading.Unit.Should().Be(config.Unit);
        reading.Value.Should().BeInRange(config.MinValue, config.MaxValue);
        reading.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        reading.Status.Should().BeOneOf("Normal", "Warning", "Critical");
    }

    [Fact]
    public void GenerateReading_ShouldClampToMinMax()
    {
        var config = new SensorConfig
        {
            SensorId = 99,
            SensorType = "Test",
            Unit = "X",
            MinValue = 0,
            MaxValue = 100,
            NominalValue = 50,
            WarningThreshold = 20,
            CriticalThreshold = 40
        };

        for (int i = 0; i < 1000; i++)
        {
            var reading = _simulator.GenerateReading(config);
            reading.Value.Should().BeInRange(config.MinValue, config.MaxValue);
        }
    }

    [Fact]
    public void GenerateReading_ShouldAssignCorrectStatus()
    {
        var config = new SensorConfig
        {
            SensorId = 99,
            SensorType = "Test",
            Unit = "X",
            MinValue = 0,
            MaxValue = 100,
            NominalValue = 50,
            WarningThreshold = 10,  // Warning: deviation >= 10
            CriticalThreshold = 30  // Critical: deviation >= 30
        };

        // Generate many readings and check that status labels are correctly derived
        var statuses = new HashSet<string>();
        for (int i = 0; i < 5000; i++)
        {
            var reading = _simulator.GenerateReading(config);
            statuses.Add(reading.Status);
            
            var deviation = Math.Abs(reading.Value - config.NominalValue);
            if (deviation >= config.CriticalThreshold)
                reading.Status.Should().Be("Critical");
            else if (deviation >= config.WarningThreshold)
                reading.Status.Should().Be("Warning");
            else
                reading.Status.Should().Be("Normal");
        }
    }

    [Fact]
    public void GenerateReading_MeanReversion_ShouldTendTowardNominal()
    {
        var config = new SensorConfig
        {
            SensorId = 99,
            SensorType = "Test",
            Unit = "X",
            MinValue = 0,
            MaxValue = 100,
            NominalValue = 50,
            WarningThreshold = 20,
            CriticalThreshold = 40
        };

        // Generate 500 readings and check average is near nominal
        double sum = 0;
        for (int i = 0; i < 500; i++)
        {
            var reading = _simulator.GenerateReading(config);
            sum += reading.Value;
        }

        var avg = sum / 500;
        avg.Should().BeApproximately(config.NominalValue, 15,
            "mean-reverting random walk should average near nominal value");
    }
}
