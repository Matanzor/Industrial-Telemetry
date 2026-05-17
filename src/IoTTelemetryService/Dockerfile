FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/IoTTelemetryService/IoTTelemetryService.csproj src/IoTTelemetryService/
RUN dotnet restore src/IoTTelemetryService/IoTTelemetryService.csproj

COPY src/IoTTelemetryService/ src/IoTTelemetryService/
RUN dotnet publish src/IoTTelemetryService/IoTTelemetryService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IoTTelemetryService.dll"]
