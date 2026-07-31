# Build context: repository root (see docker-compose.yml)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/src/NetMonitor.Api/NetMonitor.Api.csproj NetMonitor.Api/
RUN dotnet restore NetMonitor.Api/NetMonitor.Api.csproj

COPY backend/src/NetMonitor.Api/ NetMonitor.Api/
RUN dotnet publish NetMonitor.Api/NetMonitor.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "NetMonitor.Api.dll"]