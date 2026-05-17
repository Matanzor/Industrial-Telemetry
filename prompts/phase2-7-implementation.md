# Phase 2–7 — Service Implementation, Testing & Hardening

## Prompt 3: IoT Telemetry Service

```
Implement the IoT Telemetry Service as a .NET 9 Worker Service (no HTTP surface):

Sensor Simulation:
- Exactly 20 sensors: 4 each of Temperature (°C), Pressure (PSI), Humidity (%), 
  Vibration (mm/s), Speed (RPM)
- Each sensor generates one reading per second using a mean-reverting random walk 
  (drift toward nominal value with bounded noise) — produces realistic industrial data
- Per-sensor configuration: min/max physical limits, nominal operating point, 
  warning/critical deviation thresholds
- Status derived from deviation: Normal / Warning / Critical

Data Publishing (concurrent per reading):
- Redis: SET sensor:{id} with JSON payload (latest state, overwritten each second)
- RabbitMQ: Publish to "telemetry.events" fanout exchange with persistent delivery mode

Technical requirements:
- StackExchange.Redis and RabbitMQ.Client v7 (fully async API)
- PeriodicTimer for precise 1-second tick intervals
- DI with interfaces (ISensorSimulator, IRedisPublisher, IRabbitMqPublisher) for testability
- Per-sensor error handling — one sensor failure must not stop the others
- Dockerfile: multi-stage build, slim runtime image (no SDK in production)
```

## Prompt 4: SQL Data Service

```
Implement the SQL Data Service as a .NET 9 gRPC service with Entity Framework Core:

Data Persistence:
- RabbitMQ consumer: subscribe to "telemetry.events" fanout exchange with a dedicated 
  queue ("sql-data-service.telemetry"), manual acknowledgment, persist each reading 
  to MySQL via EF Core
- Pomelo MySQL provider targeting MySQL 8.0
- Auto-apply EF Core migrations on startup
- SensorReading entity with composite index on (SensorId, Timestamp) for efficient 
  time-range queries

gRPC Service (from shared protos/telemetry.proto):
- GetAllSensors: return latest reading per sensor using GroupBy + OrderByDescending
- GetSensorById: return latest reading for specific sensor, throw NOT_FOUND if missing
- GetSensorHistory: paginated query with optional time range filtering (from/to), 
  capped at 1000 results per page, ordered by timestamp descending

Technical requirements:
- IDesignTimeDbContextFactory for running EF migrations without a live database
- BackgroundService for RabbitMQ consumer with proper channel/connection lifecycle
- Dockerfile: multi-stage build with proto file COPY step, ASP.NET runtime image
```

## Prompt 5: REST API Service (UI Gateway)

```
Implement the REST API Service as the sole gateway between the UI and backend services:

REST Endpoints (the only REST communication in the system):
- GET /api/sensors — all sensors' latest state, proxied via gRPC to SQL Data Service
- GET /api/sensors/{id} — single sensor, with input validation (must be 1-20)
- GET /api/sensors/{id}/history?from=&to=&page=&pageSize= — historical data via gRPC

Real-Time Push (the only SignalR in the system):
- TelemetryHub at /hubs/telemetry
- Clients auto-join "all-sensors" group on connect
- Support per-sensor subscription: SubscribeToSensor / UnsubscribeFromSensor methods
- RabbitMQ consumer: subscribe to "telemetry.events" fanout exchange with dedicated 
  queue ("rest-api-service.telemetry"), broadcast each reading to SignalR groups — 
  this is push, NOT polling

Communication strictly follows assignment constraints:
- gRPC client (from shared proto, GrpcServices="Client") → SQL Data Service
- RabbitMQ consumer for event-driven SignalR broadcasting
- CORS configured for UI origins (localhost:3000, localhost:5173)
- No other communication patterns

Dockerfile: multi-stage build with proto file COPY, ASP.NET runtime image.
```

## Prompt 6: React + TypeScript UI

```
Implement the React + TypeScript UI with Vite. Exactly 3 pages as required:

Page 1 — Dashboard:
- 4×5 grid showing all 20 sensors with live values updated via SignalR (no polling)
- Each sensor card: current value, unit, sensor type, status badge 
  (Normal=green, Warning=yellow, Critical=red with glow effect), last update time
- Status summary bar: count of Normal / Warning / Critical sensors
- Click any sensor card → navigate to Sensor Detail page
- Connection status indicator (green dot = connected to SignalR)

Page 2 — Sensor Detail:
- Large live value display with color-coded status indicator
- Real-time line chart (recharts) showing last 60 seconds of readings
- Sensor metadata panel (ID, type, unit, current status)
- Back button to dashboard
- Subscribe to sensor-specific SignalR group for targeted updates

Page 3 — Historical Data:
- Sensor dropdown selector (1-20), datetime-local range pickers, Search button
- Line chart (recharts) of query results
- Paginated data table with all reading fields
- Queries REST API → gRPC → SQL Data Service → MySQL

Cross-cutting concerns:
- @microsoft/signalr with withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
- Singleton SignalR service with callback registration pattern
- Dark industrial theme (slate/navy backgrounds, sky-blue accents)
- react-router-dom for routing, axios for REST calls
- Dockerfile: multi-stage (Node build → nginx static serving)
- nginx.conf: proxy /api/ and /hubs/ to rest-api-service with WebSocket upgrade support
```

## Prompt 7: Unit & Integration Tests

```
Implement comprehensive test coverage for all backend services:

Unit Tests (run without any infrastructure dependencies):

SensorSimulatorTests:
- Verify exactly 20 sensor configs exist
- Verify exactly 5 sensor types with 4 sensors each
- Verify unique sensor IDs spanning 1-20
- Verify generated values are clamped within min/max bounds (1000 iterations)
- Verify status correctly derived from deviation thresholds (cross-check math)
- Verify mean-reverting walk averages near nominal value over 500 iterations

RedisPublisherTests:
- Mock IConnectionMultiplexer + IDatabase
- Verify StringSetAsync called with correct key "sensor:{id}" and JSON containing 
  the reading value

TelemetryGrpcServiceTests (using EF Core InMemory + MockServerCallContext):
- GetAllSensors returns only the latest reading per sensor (not oldest)
- GetSensorById returns latest reading for existing sensor
- GetSensorById throws RpcException(NOT_FOUND) for missing sensor
- GetSensorHistory returns correct page size and total count
- GetSensorHistory filters correctly by time range

SensorsControllerTests (using Moq for IGrpcTelemetryClient):
- GetAllSensors returns 200 with sensor list
- GetSensorById returns 200 for valid sensor, 404 for missing sensor
- GetSensorById returns 400 for invalid IDs (0, -1, 21, 100)
- GetSensorHistory returns 200 with paginated response
- GetSensorHistory returns 400 for invalid sensor ID

Integration Tests (require docker-compose running, tagged Category=Integration):
- All 20 sensors produce telemetry queryable via REST within 15 seconds
- SignalR hub delivers ReceiveTelemetry events for all 20 sensor IDs within 15 seconds
- Historical data persisted in MySQL and queryable via REST API
- Single sensor REST endpoint returns correct JSON structure

Tests use HttpClient + HubConnectionBuilder against localhost:5000, with configurable 
API_BASE_URL environment variable for CI.
```

## Prompt 8: Resilience & Container Startup Hardening

```
Add resilience to handle Docker container startup race conditions.

Problem: Services crash when Redis/RabbitMQ containers aren't immediately responsive 
despite docker-compose health checks passing. The default 
BackgroundServiceExceptionBehavior is StopHost, so any connection failure during 
ExecuteAsync kills the entire service.

Required fixes:

1. RabbitMQ connections (all 3 services: IoT publisher, SQL Data consumer, REST API 
   consumer) — wrap connection creation in exponential backoff retry loop:
   - Initial delay: 2 seconds
   - Double each attempt, cap at 30 seconds
   - Log each retry attempt at Warning level
   - Retry indefinitely until connected or cancellation requested

2. Redis connections (IoT + REST API Program.cs) — same retry pattern instead of 
   failing instantly on startup

3. UI VITE_API_URL handling — Docker sets VITE_API_URL="" (empty string). JavaScript 
   || treats empty string as falsy, causing fallback to localhost:5000 (wrong in Docker).
   Use ?? (nullish coalescing) so empty string = same-origin = nginx proxy.

4. nginx.conf WebSocket support — use `map $http_upgrade` for conditional Connection 
   header. Only send "upgrade" when the Upgrade header is present (WebSocket), not for 
   regular HTTP requests (SignalR negotiate POST). Add proxy_read_timeout 86400s for 
   long-lived WebSocket connections.
```

## Prompt 9: Historical Data Timezone Fix

```
Fix: Historical Data page returns empty results when user selects a date range.

Root cause analysis:
- GetSensorHistory in TelemetryGrpcService calls .ToUniversalTime() on parsed filter 
  timestamps
- Docker containers run in UTC, so DateTime.UtcNow produces UTC timestamps stored in 
  MySQL DATETIME columns (no timezone info)
- When read back, EF Core returns DateTimeKind.Unspecified
- The browser sends local time from datetime-local input
- Server parses it, .ToUniversalTime() may shift it depending on DateTimeKind, causing 
  comparison mismatches against stored data

Fix:
- Remove .ToUniversalTime() calls entirely — timestamps are already UTC throughout 
  the pipeline
- Use DateTimeStyles.RoundtripKind in DateTime.TryParse to preserve timezone info 
  from the input string as-is
- Filter values compare directly against stored timestamps without conversion
```
