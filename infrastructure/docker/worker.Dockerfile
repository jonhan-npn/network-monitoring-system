# Build context: repository root (see docker-compose.yml)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/src/NetworkMonitor.Worker/NetworkMonitor.Worker.csproj NetworkMonitor.Worker/
RUN dotnet restore NetworkMonitor.Worker/NetworkMonitor.Worker.csproj

COPY backend/src/NetworkMonitor.Worker/ NetworkMonitor.Worker/
RUN dotnet publish NetworkMonitor.Worker/NetworkMonitor.Worker.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NetworkMonitor.Worker.dll"]
