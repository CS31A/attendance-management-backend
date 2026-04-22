#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${ATTENDANCE_TEST_SQLSERVER_CONNECTION:-}" ]]; then
  echo "ATTENDANCE_TEST_SQLSERVER_CONNECTION is required." >&2
  exit 1
fi

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd is required to verify profile UUID anomalies." >&2
  exit 1
fi

server=""
database=""
username=""
password=""
trust_server_certificate=""

IFS=';' read -r -a connection_parts <<< "$ATTENDANCE_TEST_SQLSERVER_CONNECTION"
for part in "${connection_parts[@]}"; do
  [[ -z "${part// }" ]] && continue

  key="${part%%=*}"
  value="${part#*=}"
  normalized_key="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]' | tr -d ' ')"

  case "$normalized_key" in
    server|datasource|addr|address|networkaddress)
      server="$value"
      ;;
    initialcatalog|database)
      database="$value"
      ;;
    userid|uid|user)
      username="$value"
      ;;
    password|pwd)
      password="$value"
      ;;
    trustservercertificate)
      trust_server_certificate="$value"
      ;;
  esac
done

if [[ -z "$server" || -z "$database" || -z "$username" || -z "$password" ]]; then
  echo "ATTENDANCE_TEST_SQLSERVER_CONNECTION must include server, database, user id, and password." >&2
  exit 1
fi

sqlcmd_args=(-S "$server" -d "$database" -U "$username" -P "$password" -W -h -1 -s "|" -l 30)
if [[ "${trust_server_certificate,,}" == "true" ]]; then
  sqlcmd_args+=(-C)
fi

uuid_tables=(
  "Students"
  "Instructors"
  "Admins"
  "Courses"
  "Subjects"
  "Sections"
  "Classrooms"
  "Schedules"
  "StudentEnrollments"
  "Sessions"
  "AttendanceRecords"
  "QrCodes"
)

# Phase 8 widens the read-only anomaly gate to the expanded UUID schema surface.
# Fingerprint/device tables stay out of scope until their dedicated later-phase rollout work.
anomaly_query="SET NOCOUNT ON;"
for table_name in "${uuid_tables[@]}"; do
  if [[ "$anomaly_query" != "SET NOCOUNT ON;" ]]; then
    anomaly_query+=$'\nUNION ALL\n'
  else
    anomaly_query+=$'\n'
  fi

  anomaly_query+="SELECT '${table_name}' AS [TableName],
       (SELECT COUNT(*) FROM [${table_name}] WHERE [Uuid] IS NULL) AS [NullCount],
       (SELECT COUNT(*) FROM (SELECT [Uuid] FROM [${table_name}] WHERE [Uuid] IS NOT NULL GROUP BY [Uuid] HAVING COUNT(*) > 1) AS duplicates) AS [DuplicateCount],
       (SELECT COUNT(*) FROM [${table_name}] WHERE [Uuid] = '00000000-0000-0000-0000-000000000000') AS [ZeroGuidCount]"
done
anomaly_query+=";"

anomaly_detected=0
rows_seen=0

if ! query_output="$(sqlcmd "${sqlcmd_args[@]}" -Q "$anomaly_query" | tr -d '\r')"; then
  echo "Failed to query UUID anomaly state from SQL Server." >&2
  exit 1
fi

if [[ -z "${query_output//[$'\t\r\n ']}" ]]; then
  echo "UUID anomaly query returned no rows." >&2
  exit 1
fi

while IFS='|' read -r table_name null_count duplicate_count zero_guid_count; do
  [[ -z "${table_name// }" ]] && continue

  table_name="${table_name//[$'\t\r\n ']/}"
  null_count="${null_count//[$'\t\r\n ']/}"
  duplicate_count="${duplicate_count//[$'\t\r\n ']/}"
  zero_guid_count="${zero_guid_count//[$'\t\r\n ']/}"

  if [[ ! "$null_count" =~ ^[0-9]+$ || ! "$duplicate_count" =~ ^[0-9]+$ || ! "$zero_guid_count" =~ ^[0-9]+$ ]]; then
    echo "UUID anomaly query returned unexpected output for table '$table_name'." >&2
    exit 1
  fi

  rows_seen=$((rows_seen + 1))

  printf '%s: null=%s duplicate=%s zero=%s\n' "$table_name" "$null_count" "$duplicate_count" "$zero_guid_count"

  if (( null_count > 0 || duplicate_count > 0 || zero_guid_count > 0 )); then
    anomaly_detected=1
  fi
done <<< "$query_output"

expected_rows="${#uuid_tables[@]}"

if (( rows_seen != expected_rows )); then
  echo "UUID anomaly query returned $rows_seen row(s); expected $expected_rows." >&2
  exit 1
fi

if (( anomaly_detected > 0 )); then
  echo "UUID anomaly gate failed. Fix the reported rows before rollout." >&2
  exit 1
fi

echo "UUID anomaly gate passed for Wave 1 profile tables and Phase 8 Slice A/Slice B UUID tables."
