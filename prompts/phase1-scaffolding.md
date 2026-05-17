# Phase 1 — Architecture Design & Project Scaffolding

## Prompt 1: System Architecture & Planning

> **Context:** Provided the full assignment PDF (Industrial Real-Time System) and the recruiter's email explaining the focus on architectural reasoning and trade-offs.

```
Read the attached assignment PDF in full and understand all 8 sections of requirements.

Design a complete system architecture for this industrial real-time telemetry system. I need:

1. Service decomposition — which services, what each owns, their responsibilities
2. Data flow diagram — how telemetry moves from sensor simulation → Redis → RabbitMQ → 
   persistence (MySQL) and real-time delivery (SignalR → UI)
3. Communication pattern map — which protocols connect which services, ensuring:
   - Backend services communicate exclusively via gRPC and RabbitMQ
   - SignalR only between REST API and UI
   - REST only between UI and REST API
4. Infrastructure components — Redis, RabbitMQ, MySQL with clear roles:
   - Redis = latest sensor state (one key per sensor, overwritten each second)
   - RabbitMQ = event bus (fanout exchange so multiple consumers get every message)
   - MySQL = historical persistence (append-only time-series data)
5. UI page design — exactly 3 pages with meaningful content for an industrial monitoring context
6. Technology choices — language/framework for each service with justification
7. Full project folder structure

Also document:
- Prerequisites I need to install (Docker, .NET SDK, Node.js, etc.)
- The phase-by-phase implementation plan
- Key architectural decisions and trade-offs for the README

Plan this thoroughly — the recruiter emphasized that architectural reasoning matters more than 
just working code.
```

## Prompt 2: Scaffold Project Infrastructure

```
Initialize the full project structure:

1. Create Landa.sln with all project references
2. Scaffold .NET 9 projects:
   - IoTTelemetryService (Worker Service) — no HTTP surface, background processing only
   - SqlDataService (gRPC Service) — serves proto-defined RPCs, owns MySQL schema
   - RestApiService (Web API) — REST controllers + SignalR hub, sole UI gateway
3. Scaffold React + TypeScript UI with Vite (react-ts template)
4. Create shared protos/telemetry.proto defining:
   - TelemetryService with GetAllSensors, GetSensorById, GetSensorHistory RPCs
   - Request/response messages with proper field types and pagination support
5. Create docker-compose.yml with:
   - Redis 7 (Alpine), RabbitMQ 3 with management UI, MySQL 8.0
   - All 4 application services with proper build contexts
   - Health checks, depends_on conditions, shared Docker network
   - Environment variables for all connection strings
6. Create .gitignore for .NET, Node, Docker, IDE files
7. Install NuGet packages per service:
   - IoT: StackExchange.Redis, RabbitMQ.Client
   - SQL Data: Pomelo.EntityFrameworkCore.MySql, Grpc.AspNetCore, RabbitMQ.Client
   - REST API: Grpc.Net.Client, Grpc.Net.ClientFactory, Microsoft.AspNetCore.SignalR, 
     StackExchange.Redis, RabbitMQ.Client
8. Create xUnit test projects with Moq + FluentAssertions for each service + IntegrationTests

Verify everything compiles with `dotnet build`.
```
