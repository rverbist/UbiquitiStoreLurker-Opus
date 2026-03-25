# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

# Install Node.js for Vite build
RUN apk add --no-cache nodejs npm

# Restore NuGet
COPY UbiquitiStoreLurker.Web/UbiquitiStoreLurker.Web.csproj UbiquitiStoreLurker.Web/
COPY UbiquitiStoreLurker.ServiceDefaults/UbiquitiStoreLurker.ServiceDefaults.csproj UbiquitiStoreLurker.ServiceDefaults/
COPY UbiquitiStoreLurker.Tests/UbiquitiStoreLurker.Tests.csproj UbiquitiStoreLurker.Tests/
COPY Directory.Build.props ./
RUN dotnet restore UbiquitiStoreLurker.Web/UbiquitiStoreLurker.Web.csproj

# Build Vue app
COPY UbiquitiStoreLurker.Web/ClientApp/ UbiquitiStoreLurker.Web/ClientApp/
RUN cd UbiquitiStoreLurker.Web/ClientApp && npm ci && npm run build

# Build .NET app
COPY UbiquitiStoreLurker.Web/ UbiquitiStoreLurker.Web/
COPY UbiquitiStoreLurker.ServiceDefaults/ UbiquitiStoreLurker.ServiceDefaults/
RUN dotnet publish UbiquitiStoreLurker.Web/UbiquitiStoreLurker.Web.csproj \
    -c Release -o /app --no-restore

# Stage 2: Test
FROM build AS test
COPY UbiquitiStoreLurker.Tests/ UbiquitiStoreLurker.Tests/
RUN dotnet restore UbiquitiStoreLurker.Tests/UbiquitiStoreLurker.Tests.csproj
RUN dotnet test UbiquitiStoreLurker.Tests/UbiquitiStoreLurker.Tests.csproj \
    --no-restore -c Release --logger "console;verbosity=normal"

# NOTE: UbiquitiStoreLurker.ServiceDefaults and UbiquitiStoreLurker.AppHost are intentionally excluded
# from this image. The Dockerfile builds only UbiquitiStoreLurker.Web/ — Aspire packages are
# dev-only tooling. Production observability: prometheus-net → Grafana lab stack (Phase 10).
# Verify: docker run --rm --entrypoint sh ubiquitistorelurker:latest \
#   -c 'ls /app/*.dll | grep -i aspire && echo FAIL || echo PASS' → should print PASS

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Create non-root user (UID 10001 avoids conflicts with Alpine system accounts)
RUN addgroup -S appgroup && adduser -S -G appgroup -u 10001 appuser && \
    mkdir -p /data /logs && \
    chown appuser:appgroup /data /logs

COPY --from=build /app ./
COPY --from=build --chown=appuser:appuser /app/wwwroot ./wwwroot

USER appuser
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/health/live || exit 1

ENTRYPOINT ["dotnet", "UbiquitiStoreLurker.Web.dll"]
