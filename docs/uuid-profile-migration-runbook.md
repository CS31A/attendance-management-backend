# UUID Profile Migration Runbook

This runbook covers Phase 3 rollout for additive `Uuid` columns on `Students`, `Instructors`, and `Admins`.

## Scope

- Applies the `AddWave1ProfileUuidColumns` migration.
- Adds database-owned `Uuid` values with `NEWSEQUENTIALID()`.
- Preserves existing integer `Id` primary keys, `StudentEnrollment.StudentId`, stored procedures, and routes.
- Exposes additive `Uuid` values in API response contracts (`UserProfileResponseDto`, `GetAllUsersDto`).

## Migration Posture

- **Forward-only migration:** do not rely on `dotnet ef database update <previous>` for rollback after backfill starts.
- **Fallback posture:** restore the database from a backup taken before rollout.
- **Stop rollout immediately** if any anomaly check below fails.

## Prerequisites

1. Confirm the deployment window covers one coordinated migration for `Students`, `Instructors`, and `Admins`.
2. Confirm the target environment points to the intended SQL Server database.
3. Take a full database backup or snapshot **before** running the migration.
4. Record the backup name, timestamp, operator, and environment in the deployment log.

## Pre-Migration Backup Requirement

Do not continue without a restorable backup. Example posture:

- SQL Server full backup, managed snapshot, or equivalent platform snapshot
- Backup verification performed according to your environment standard
- Restore target identified before rollout begins

If a valid backup cannot be produced or verified, **stop rollout**.

## Rollout Steps

1. Build the backend artifacts:

   ```bash
   dotnet build attendance_monitoring/attendance_monitoring.csproj
   ```

2. Apply the migration:

   ```bash
   dotnet ef database update --project attendance_monitoring/attendance_monitoring.csproj --connection "<sql-server-connection-string>"
   ```

3. Run the anomaly checks below against the migrated database before handing the environment back to normal traffic.

## Post-Migration SQL Checks

Run these checks after the migration completes. Each query must return **zero rows**.

### Null `Uuid` check

```sql
SELECT 'Students' AS [TableName], [Id], [Uuid] FROM [Students] WHERE [Uuid] IS NULL
UNION ALL
SELECT 'Instructors' AS [TableName], [Id], [Uuid] FROM [Instructors] WHERE [Uuid] IS NULL
UNION ALL
SELECT 'Admins' AS [TableName], [Id], [Uuid] FROM [Admins] WHERE [Uuid] IS NULL;
```

### Duplicate `Uuid` check

```sql
SELECT 'Students' AS [TableName], [Uuid], COUNT(*) AS [DuplicateCount]
FROM [Students]
GROUP BY [Uuid]
HAVING COUNT(*) > 1
UNION ALL
SELECT 'Instructors' AS [TableName], [Uuid], COUNT(*) AS [DuplicateCount]
FROM [Instructors]
GROUP BY [Uuid]
HAVING COUNT(*) > 1
UNION ALL
SELECT 'Admins' AS [TableName], [Uuid], COUNT(*) AS [DuplicateCount]
FROM [Admins]
GROUP BY [Uuid]
HAVING COUNT(*) > 1;
```

### Contract-boundary sanity check

Confirm legacy integer boundaries remain unchanged:

- `StudentEnrollment.StudentId` still references `Students.Id`
- API response contracts expose additive `Uuid` fields alongside existing integer profile IDs

## Stop Rollout Conditions

Stop rollout and keep the environment out of normal traffic if any of the following occurs:

- Migration command fails at any point
- Null-check query returns any row
- Duplicate-check query returns any row
- A target table is missing the `Uuid` default or unique index after migration
- Any verification shows `StudentEnrollment` or other legacy-int contract surfaces changed unexpectedly

## Restore-Based Recovery

If rollout stops after the migration has started, treat recovery as restore-only:

1. Stop further application deployments against the affected database.
2. Restore the database from the pre-migration backup/snapshot.
3. Validate application connectivity and legacy contract behavior on the restored database.
4. Investigate the anomaly before scheduling another migration attempt.

## Notes For Later Phases

- Phase 3 covers both the schema/data migration and the API contract exposure of additive `Uuid` fields.
- Do not repurpose the Phase 3 migration to change legacy integer key relationships or stored-procedure signatures.
