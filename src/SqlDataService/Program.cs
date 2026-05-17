using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;
using SqlDataService.Services;

var builder = WebApplication.CreateBuilder(args);

// MySQL via EF Core
var connectionString = builder.Configuration.GetConnectionString("MySql")
    ?? "Server=localhost;Port=3306;Database=telemetrydb;User=telemetry;Password=Telemetry_pass1;";
builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// gRPC
builder.Services.AddGrpc();

// RabbitMQ consumer (background service)
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGrpcService<TelemetryGrpcService>();
app.MapGet("/", () => "SQL Data Service (gRPC)");

app.Run();
