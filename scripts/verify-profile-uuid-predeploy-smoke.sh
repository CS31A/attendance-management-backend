#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

IMAGE="${ATTENDANCE_TEST_SQLSERVER_IMAGE:-mcr.microsoft.com/mssql/server:2022-latest}"
PASSWORD="${ATTENDANCE_TEST_SQLSERVER_PASSWORD:-YourStrong!Passw0rd}"
PORT="${ATTENDANCE_TEST_SQLSERVER_PORT:-14335}"
DB_NAME="${ATTENDANCE_UUID_SMOKE_DB:-attendance_uuid_predeploy_smoke_$(date +%s)_$$}"
CONTAINER_NAME="attendance-uuid-predeploy-smoke-$(date +%s)-$$"
KEEP_CONTAINER="${ATTENDANCE_UUID_SMOKE_KEEP_CONTAINER:-false}"

cleanup() {
  if [[ "${KEEP_CONTAINER,,}" == "true" ]]; then
    echo "Keeping container ${CONTAINER_NAME} for inspection (ATTENDANCE_UUID_SMOKE_KEEP_CONTAINER=true)."
    return
  fi

  podman stop "$CONTAINER_NAME" >/dev/null 2>&1 || true
}

trap cleanup EXIT

if ! command -v podman >/dev/null 2>&1; then
  echo "podman is required to run SQL Server smoke verification." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is required to apply migrations for smoke verification." >&2
  exit 1
fi

if [[ ! "$DB_NAME" =~ ^[A-Za-z0-9_]+$ ]]; then
  echo "ATTENDANCE_UUID_SMOKE_DB must contain only letters, numbers, and underscores." >&2
  exit 1
fi

podman run -d --rm \
  --name "$CONTAINER_NAME" \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD="$PASSWORD" \
  -p "127.0.0.1:${PORT}:1433" \
  "$IMAGE" >/dev/null

echo "Waiting for SQL Server in container ${CONTAINER_NAME} on port ${PORT}..."
for _ in $(seq 1 60); do
  if podman exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

if ! podman exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
  echo "SQL Server container did not become ready in time." >&2
  exit 1
fi

echo "Creating smoke database ${DB_NAME}..."
podman exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P "$PASSWORD" \
  -C \
  -Q "IF DB_ID(N'${DB_NAME}') IS NULL CREATE DATABASE [${DB_NAME}];" >/dev/null

ATTENDANCE_TEST_SQLSERVER_CONNECTION="Server=127.0.0.1,${PORT};Initial Catalog=${DB_NAME};User ID=sa;Password=${PASSWORD};TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
export ATTENDANCE_TEST_SQLSERVER_CONNECTION

echo "Applying migrations to ${DB_NAME}..."
dotnet ef database update \
  --project attendance_monitoring/attendance_monitoring.csproj \
  --startup-project attendance_monitoring/attendance_monitoring.csproj \
  --connection "$ATTENDANCE_TEST_SQLSERVER_CONNECTION"

echo "Running UUID anomaly predeploy gate against ${DB_NAME}..."
./scripts/verify-profile-uuid-predeploy.sh

echo "Smoke verification completed successfully."
