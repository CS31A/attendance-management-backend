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

connection_string="$ATTENDANCE_TEST_SQLSERVER_CONNECTION"

while [[ -n "$connection_string" ]]; do
  # Trim leading whitespace and semicolons
  connection_string="${connection_string#"${connection_string%%[![:space:];]*}"}"
  [[ -z "$connection_string" ]] && break

  # Need at least one equals sign to form a key-value pair
  if [[ "$connection_string" != *"="* ]]; then
    break
  fi

  key="${connection_string%%=*}"
  rest="${connection_string#*=}"

  # Determine if value is quoted and extract accordingly
  if [[ "$rest" == \'* ]]; then
    rest="${rest#\'}"
    if [[ "$rest" == *\'* ]]; then
      IFS="'" read -r prefix _ <<< "$rest"
      value="$prefix"
      connection_string="${rest:$(( ${#prefix} + 1 ))}"
      connection_string="${connection_string#;}"
    else
      # Missing closing quote – consume rest as value
      value="$rest"
      connection_string=""
    fi
  elif [[ "$rest" == \"* ]]; then
    rest="${rest#\"}"
    if [[ "$rest" == *\"* ]]; then
      IFS='"' read -r prefix _ <<< "$rest"
      value="$prefix"
      connection_string="${rest:$(( ${#prefix} + 1 ))}"
      connection_string="${connection_string#;}"
    else
      value="$rest"
      connection_string=""
    fi
  else
    # Unquoted value – terminated by semicolon or end of string
    value="${rest%%;*}"
    connection_string="${rest:$(( ${#value} ))}"
    connection_string="${connection_string#;}"
  fi

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
  "Fingerprints"
  "FingerprintDevices"
  "FingerprintEnrollmentSessions"
  "FingerprintScanEvents"
)

# Phase 8 widens the read-only anomaly gate to the full expanded UUID schema surface,
# including the fingerprint-support tables added in the gap-closure follow-up.
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

echo "UUID anomaly gate passed for Wave 1 profile tables and all Phase 8 UUID tables, including fingerprint-support tables."
