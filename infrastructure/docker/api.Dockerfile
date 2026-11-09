# Build context: repository root (see docker-compose.yml)

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, copying only the project files (including every referenced
# project) so Docker can cache the restore layer when only source changes.
COPY backend/src/NetworkMonitor.Domain/NetworkMonitor.Domain.csproj NetworkMonitor.Domain/
COPY backend/src/NetworkMonitor.Shared/NetworkMonitor.Shared.csproj NetworkMonitor.Shared/
COPY backend/src/NetworkMonitor.Application/NetworkMonitor.Application.csproj NetworkMonitor.Application/
COPY backend/src/NetworkMonitor.Infrastructure/NetworkMonitor.Infrastructure.csproj NetworkMonitor.Infrastructure/
COPY backend/src/NetworkMonitor.API/NetworkMonitor.API.csproj NetworkMonitor.API/
RUN dotnet restore NetworkMonitor.API/NetworkMonitor.API.csproj

# Now copy the source for the API and all of its project references.
COPY backend/src/NetworkMonitor.Domain/ NetworkMonitor.Domain/
COPY backend/src/NetworkMonitor.Shared/ NetworkMonitor.Shared/
COPY backend/src/NetworkMonitor.Application/ NetworkMonitor.Application/
COPY backend/src/NetworkMonitor.Infrastructure/ NetworkMonitor.Infrastructure/
COPY backend/src/NetworkMonitor.API/ NetworkMonitor.API/
RUN dotnet publish NetworkMonitor.API/NetworkMonitor.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NetworkMonitor.API.dll"]
