using RestApiService.Hubs;
using RestApiService.Services;
using Shared.Grpc;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Redis (with retry)
var redisConnStr = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
    var retryDelay = TimeSpan.FromSeconds(2);
    while (true)
    {
        try
        {
            var mux = ConnectionMultiplexer.Connect(redisConnStr);
            logger.LogInformation("Connected to Redis");
            return mux;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to Redis, retrying in {Delay}s...", retryDelay.TotalSeconds);
            Thread.Sleep(retryDelay);
            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
        }
    }
});

// gRPC client to SQL Data Service
var grpcUrl = builder.Configuration["Grpc:SqlDataServiceUrl"] ?? "http://localhost:5001";
builder.Services.AddGrpcClient<TelemetryService.TelemetryServiceClient>(options =>
{
    options.Address = new Uri(grpcUrl);
});
builder.Services.AddScoped<IGrpcTelemetryClient, GrpcTelemetryClient>();

// SignalR
builder.Services.AddSignalR();

// RabbitMQ consumer → SignalR broadcaster
builder.Services.AddHostedService<RabbitMqConsumer>();

// Controllers
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();
