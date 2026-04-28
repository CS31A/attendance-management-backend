#!/usr/bin/env bash
set -euo pipefail

IMAGE="${ATTENDANCE_TEST_SQLSERVER_IMAGE:-mcr.microsoft.com/mssql/server:2022-latest}"
PASSWORD="${ATTENDANCE_TEST_SQLSERVER_PASSWORD:-YourStrong!Passw0rd}"
BASE_DB="${ATTENDANCE_TEST_SQLSERVER_BASE_DB:-attendance_integration_base}"
CONTAINER_NAME="attendance-test-sqlserver-$(date +%s)-$$"
PORT="${ATTENDANCE_TEST_SQLSERVER_PORT:-14333}"
FILTER="${1:-}"

cleanup() {
  podman stop "$CONTAINER_NAME" >/dev/null 2>&1 || true
}

trap cleanup EXIT

if ! command -v podman >/dev/null 2>&1; then
  echo "podman is required to run SQL Server integration tests." >&2
  exit 1
fi

podman run -d --rm \
  --name "$CONTAINER_NAME" \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD="$PASSWORD" \
  -p "127.0.0.1:${PORT}:1433" \
  "$IMAGE" >/dev/null

export ATTENDANCE_TEST_SQLSERVER_CONNECTION="Server=127.0.0.1,${PORT};Initial Catalog=${BASE_DB};User ID=sa;Password=${PASSWORD};TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"

echo "Waiting for SQL Server in container ${CONTAINER_NAME} on port ${PORT}..."
for _ in $(seq 1 60); do
  if podman exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    echo "SQL Server is ready."
    if [[ -n "$FILTER" ]]; then
      dotnet test attendance.testproject/attendance.testproject.csproj --filter "$FILTER"
    else
      dotnet test attendance.testproject/attendance.testproject.csproj
    fi
    exit 0
  fi
  sleep 2
done

echo "SQL Server container did not become ready in time." >&2
exit 1
