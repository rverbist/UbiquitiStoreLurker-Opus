#!/usr/bin/env bash
# Deploy UbiquitiStoreLurker to the production Docker stack.
#
# Builds the image, syncs compose files to /opt/docker/apps/ubiquitistorelurker/,
# restarts the production stack, waits for /api/health/ready, then prints live URLs.
set -euo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROD="/opt/docker/apps/ubiquitistorelurker"

echo "==> Source at      $SRC"
echo "==> Deploying to   $PROD"

echo '==> Building image...'
docker build -t ubiquitistorelurker:latest "$SRC"

echo '==> Stopping local dev containers...'
docker compose -f "$SRC/docker-compose.yml" down 2>/dev/null || true

echo '==> Syncing compose files...'
sudo mkdir -p "$PROD"
sudo rsync -av --checksum "$SRC/docker-compose.yml" "$PROD/"
sudo rsync -av --checksum "$SRC/otel-collector.yaml" "$PROD/"

echo '==> Stopping old production containers...'
sudo docker compose -f "$PROD/docker-compose.yml" down --remove-orphans 2>/dev/null || true

echo '==> Starting production containers...'
sudo docker compose -f "$PROD/docker-compose.yml" up -d --no-build

echo '==> Waiting for readiness...'
until curl -sf https://ubiquitistorelurker.rverbist.io/api/health/ready >/dev/null 2>&1; do
    printf '.'
    sleep 2
done
echo ' HEALTHY'

echo 'App:              https://ubiquitistorelurker.rverbist.io'
echo 'Aspire Dashboard: https://aspire.ubiquitistorelurker.rverbist.io'
echo 'Grafana:          https://grafana.rverbist.io'
