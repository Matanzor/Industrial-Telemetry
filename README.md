# Industrial Real-Time Sensor Telemetry System

A distributed real-time system that simulates 20 industrial sensors, processes telemetry through Redis and RabbitMQ, persists data in MySQL, and delivers live updates to a React dashboard via SignalR.

## Quick Start

### Prerequisites
| Tool | Version | Download |
|------|---------|----------|
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop/ |
| .NET 9 SDK | 9.0+ | https://dotnet.microsoft.com/download/dotnet/9.0 |
| Node.js | 20 LTS | https://nodejs.org/ |
| Git | Latest | https://git-scm.com/ |

### Run the Full System
```bash
docker-compose up --build
```

Then open http://localhost:3000 in your browser.

### Run Unit Tests
```bash
dotnet test Landa.sln --filter "Category!=Integration"
```

### Run Integration Tests (requires docker-compose running)
```bash
docker-compose up -d --build
# Wait ~30 seconds for services to start
dotnet test tests/IntegrationTests --filter "Category=Integration"
```

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         Browser (React + TypeScript)                  │
│  ┌──────────────┐  ┌─────────────────┐  ┌────────────────────────┐  │
│  │  Dashboard    │  │  Sensor Detail  │  │  Historical Data       │  │
│  │  (4×5 grid)   │  │  (live chart)   │  │  (date range + table)  │  │
│  └──────┬───────┘  └───────┬─────────┘  └──────────┬─────────────┘  │
│         │ SignalR           │ SignalR                │ REST           │
└─────────┼──────────────────┼────────────────────────┼────────────────┘
          │                  │                        │
          ▼                  ▼                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    REST API Service (C# / ASP.NET)                   │
│  ┌──────────────┐  ┌──────────────────┐  ┌───────────────────────┐  │
│  │  SignalR Hub  │  │  REST Controllers │  │  gRPC Client         │  │
│  │  (push to UI)│  │  /api/sensors/*   │  │  (query SQL Data Svc)│  │
│  └──────▲───────┘  └──────────────────┘  └──────────┬────────────┘  │
│         │                                           │ gRPC           │
│  ┌──────┴───────────┐                               │                │
│  │  RabbitMQ Consumer│                               │                │
│  └──────▲────────────┘                               │                │
└─────────┼────────────────────────────────────────────┼───────────────┘
          │ RabbitMQ                                   │
          │ (fanout)                                   ▼
┌─────────┴───────────────────────┐   ┌───────────────────────────────┐
│     IoT Telemetry Service        │   │     SQL Data Service           │
│     (C# Worker Service)         │   │     (C# gRPC + EF Core)       │
│  ┌────────────────────────────┐ │   │  ┌──────────────────────────┐ │
│  │  SensorSimulator (20 sensors│ │   │  │  gRPC Service            │ │
│  │  1 reading/sec each)       │ │   │  │  (GetAll, GetById,       │ │
│  └─────────┬──────────────────┘ │   │  │   GetHistory)            │ │
│            │                    │   │  └──────────────────────────┘ │
│  ┌─────────▼──────┐            │   │  ┌──────────────────────────┐ │
│  │ Redis Publisher │            │   │  │  RabbitMQ Consumer       │ │
│  │ (SET sensor:id) │            │   │  │  → EF Core → MySQL      │ │
│  └─────────┬──────┘            │   │  └──────────▲───────────────┘ │
│  ┌─────────▼──────────────┐    │   └─────────────┼─────────────────┘
│  │ RabbitMQ Publisher      │    │                 │ RabbitMQ
│  │ (fanout exchange)       │────┼─────────────────┘
│  └─────────────────────────┘    │
└─────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Infrastructure                                │
│  ┌──────────┐     ┌──────────────┐     ┌──────────────────────────┐ │
│  │  Redis    │     │  RabbitMQ    │     │  MySQL 8.0               │ │
│  │  (latest  │     │  (fanout     │     │  (historical readings)   │ │
│  │   state)  │     │   exchange)  │     │                          │ │
│  └──────────┘     └──────────────┘     └──────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### Real-Time Path (Redis → API → UI)
1. **IoT Telemetry Service** generates a reading for each of the 20 sensors every second
2. Each reading is written to **Redis** as `sensor:{id}` (latest state)
3. Simultaneously published to **RabbitMQ** `telemetry.events` fanout exchange
4. **REST API Service** consumes the RabbitMQ message and pushes it to all connected UI clients via **SignalR**
5. The React **UI** receives the event and updates the dashboard in real-time (no polling)

### Persistence Path (RabbitMQ → SQL Data Service → MySQL)
1. **SQL Data Service** consumes the same RabbitMQ message (separate queue, same fanout exchange)
2. Each reading is persisted to **MySQL** via Entity Framework Core
3. Historical data is queryable via **gRPC** from the REST API Service

### Query Path (UI → REST → gRPC → MySQL)
1. **UI** sends REST request to `GET /api/sensors/{id}/history?from=...&to=...`
2. **REST API Service** calls **SQL Data Service** via gRPC (`GetSensorHistory`)
3. **SQL Data Service** queries MySQL and returns paginated results

---

## Why gRPC, RabbitMQ, and SignalR?

| Technology | Used Where | Why |
|------------|-----------|-----|
| **gRPC** | Between REST API ↔ SQL Data Service | Binary protocol (Protobuf) for efficient service-to-service communication. Strongly typed contracts via `.proto` file. HTTP/2 with multiplexing. |
| **RabbitMQ** | IoT Service → SQL Data Service, IoT Service → REST API Service | Asynchronous event bus. Fanout exchange ensures both consumers get every message independently. Decouples producers from consumers. Message persistence for reliability. |
| **SignalR** | REST API Service → UI | WebSocket-based real-time push to the browser. No polling required. Auto-reconnect built-in. Group-based subscription (per-sensor or all-sensors). |
| **REST** | UI → REST API Service | Standard HTTP for CRUD operations. Browser-friendly. Easy to test with curl/Postman. |

**Why not use gRPC for everything?** Browsers can't call gRPC directly (requires gRPC-Web proxy). REST is simpler for the UI to consume.

**Why not use SignalR between backend services?** SignalR is designed for client-server communication with WebSocket fallback. gRPC is more efficient for server-to-server calls. RabbitMQ provides durable message delivery that SignalR doesn't.

---

## Key Trade-offs and Constraints

| Decision | Trade-off |
|----------|-----------|
| All-C# backend | Consistency over best-tool-for-job. Python might prototype faster; C++ might be more performant for IoT. |
| Redis for latest state only | Simple and fast, but no built-in time-series features. MySQL handles history. |
| Fanout exchange | Every consumer gets every message. Simple at 20 sensors/sec, but doesn't scale to millions without partitioning. |
| JSON in RabbitMQ messages | Human-readable and debuggable, but less efficient than Protobuf. Fine at this scale. |
| EF Core for MySQL | Familiar ORM, but adds overhead vs. raw SQL. Acceptable for this throughput. |
| Single docker-compose | Easy to run locally, but not production-ready. Would need Kubernetes for real deployment. |

---

## Scaling to More Sensors / Higher Rates

| Scale | Change Required |
|-------|----------------|
| **100 sensors** | No changes needed. Current architecture handles it. |
| **1,000 sensors** | Batch inserts in SQL Data Service. Redis Cluster. SignalR Redis backplane for multiple API instances. |
| **10,000+ sensors** | Partition RabbitMQ queues by sensor range. TimescaleDB or ClickHouse instead of MySQL. Kafka instead of RabbitMQ. Downsample data for UI. |
| **Higher update rates (100Hz)** | Buffer + batch at every stage. Write-ahead log in IoT service. Aggregate before publishing to RabbitMQ. Time-bucketed SignalR updates. |

---

## Failure Scenarios

| Failure | Impact | Recovery |
|---------|--------|----------|
| **Redis down** | Latest state unavailable. IoT service logs errors but continues publishing to RabbitMQ. Real-time flow and persistence unaffected. | IoT service reconnects automatically. Redis state rebuilds within 1 second. |
| **RabbitMQ down** | No real-time updates to UI. No new data persisted. IoT service buffers or drops messages. | All services retry with exponential backoff (2s → 30s cap). Unacknowledged messages re-delivered. Manual ack ensures no data loss. |
| **MySQL down** | Historical queries fail. SQL Data Service consumer pauses. | EF Core retry policy. Messages queue up in RabbitMQ until MySQL recovers. |
| **REST API restarts** | SignalR clients disconnect. | Clients auto-reconnect (configured with exponential backoff). State rebuilds from next telemetry push. |
| **IoT Service restarts** | Gap in telemetry (~1-2 sec). | Stateless service. Restarts immediately and resumes generating data. |

---

## Observability (Production Recommendations)

**Structured Logging:** Each service logs to stdout (Docker captures). Recommend aggregation with ELK or Loki.

**Metrics to add:**
- `telemetry_readings_generated_total` (counter, per sensor)
- `rabbitmq_publish_duration_seconds` (histogram)
- `mysql_insert_duration_seconds` (histogram)
- `signalr_connected_clients` (gauge)
- `grpc_request_duration_seconds` (histogram, per method)

**Health checks:** Docker Compose health checks configured for Redis, RabbitMQ, MySQL. Services should expose `/health` endpoints.

**Alerting rules:**
- No telemetry from a sensor for >5 seconds
- RabbitMQ queue depth >1000 messages
- MySQL replication lag >1 second
- SignalR connection count = 0

---

## Sensor Behavior Handling

| Behavior | Current Approach | Optimization |
|----------|-----------------|-------------|
| **Fast-changing sensors** | Published every second regardless | Publish at sensor-specific rate. Batch fast readings. |
| **Slow-changing sensors** | Published every second with near-identical values | Dead-band filtering: only publish when value changes > threshold. Saves bandwidth and storage. |
| **Constant sensors** | Published every second with same value | Heartbeat mode: publish once per minute if unchanged. Store "last changed" timestamp. |

---

## Stable vs. Likely to Change

| Stable | Likely to Change |
|--------|-----------------|
| Communication patterns (gRPC, RabbitMQ, SignalR) | Sensor types and configurations |
| Core data model (sensorId, value, timestamp) | UI design and additional pages |
| 3-service backend architecture | Database schema (new fields, indexes) |
| Docker-based deployment | Message serialization format |

---

## Alternative Designs

| Current | Alternative | When to Switch |
|---------|------------|----------------|
| MySQL | TimescaleDB | >1000 sensors or need time-series aggregations |
| RabbitMQ | Apache Kafka | Need message replay, higher throughput, or stream processing |
| Redis (latest state) | Redis Streams | Need short-term history in Redis with automatic eviction |
| Fanout exchange | Topic exchange | Need selective routing (e.g., only temperature to specific consumers) |
| SignalR | Server-Sent Events | One-directional push only, simpler protocol |
| EF Core | Dapper | Need raw SQL performance for high-throughput inserts |

---

## Project Structure

```
landa/
├── docker-compose.yml
├── Landa.sln
├── .github/workflows/ci.yml
├── protos/telemetry.proto
├── prompts/                       # AI prompt documentation
├── docs/interview-qa.md           # Interview preparation Q&A
├── src/
│   ├── IoTTelemetryService/       # C# Worker Service
│   ├── SqlDataService/            # C# gRPC + EF Core
│   ├── RestApiService/            # C# Web API + SignalR
│   └── ui/                        # React + TypeScript (Vite)
└── tests/
    ├── IoTTelemetryService.Tests/ # Unit tests
    ├── SqlDataService.Tests/      # Unit tests
    ├── RestApiService.Tests/      # Unit tests
    └── IntegrationTests/          # E2E integration tests
```

---

## Services & Ports

| Service | Port | Purpose |
|---------|------|---------|
| UI | 3000 | React dashboard |
| REST API | 5000 | REST + SignalR hub |
| SQL Data Service | 5001 | gRPC server |
| Redis | 6379 | Latest sensor state |
| RabbitMQ | 5672 / 15672 | Message broker / Management UI |
| MySQL | 3306 | Historical data |

---

## Testing

### Unit Tests (23 tests)
- **SensorSimulatorTests** — Validates 20 sensors, 5 types, unique IDs, value clamping, status derivation, mean reversion
- **RedisPublisherTests** — Verifies Redis interaction
- **TelemetryGrpcServiceTests** — Tests gRPC methods with InMemory database
- **SensorsControllerTests** — Tests REST endpoints with mocked dependencies

### Integration Tests (4 tests)
- All 20 sensors produce telemetry within 10 seconds
- SignalR receives real-time updates for all 20 sensors
- Historical data is persisted and queryable
- REST API returns correct single-sensor data

Run all tests: `dotnet test Landa.sln`
