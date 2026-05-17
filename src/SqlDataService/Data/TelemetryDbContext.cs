using Microsoft.EntityFrameworkCore;
using SqlDataService.Models;

namespace SqlDataService.Data;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SensorType).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasIndex(e => e.SensorId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.SensorId, e.Timestamp });
        });
    }
}
