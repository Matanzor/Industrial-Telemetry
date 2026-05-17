FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY protos/ protos/
COPY src/SqlDataService/SqlDataService.csproj src/SqlDataService/
RUN dotnet restore src/SqlDataService/SqlDataService.csproj

COPY src/SqlDataService/ src/SqlDataService/
RUN dotnet publish src/SqlDataService/SqlDataService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SqlDataService.dll"]
