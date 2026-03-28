# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first for layer caching
# Directory.Build.props must be copied first — it defines TargetFramework
COPY Directory.Build.props .
COPY UniFiStoreWatcher.Web/UniFiStoreWatcher.Web.csproj UniFiStoreWatcher.Web/
RUN dotnet restore UniFiStoreWatcher.Web/UniFiStoreWatcher.Web.csproj

# Copy source and build
COPY UniFiStoreWatcher.Web/ UniFiStoreWatcher.Web/
WORKDIR /src/UniFiStoreWatcher.Web
RUN dotnet publish UniFiStoreWatcher.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security (Debian/Ubuntu-based image)
RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --no-create-home appuser

# Persistent log directory — mount as volume in production
RUN mkdir -p /logs && chown appuser:appgroup /logs

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "UniFiStoreWatcher.Web.dll"]
