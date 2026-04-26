using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class PromoteUuidToGuidPrimaryKeys : Migration
    {
        private static readonly string[] TargetTables =
        [
            "Admins",
            "Students",
            "Instructors",
            "Courses",
            "Subjects",
            "Sections",
            "Classrooms",
            "Schedules",
            "StudentEnrollments",
            "Sessions",
            "AttendanceRecords",
            "QrCodes",
            "Fingerprints",
            "FingerprintDevices",
            "FingerprintEnrollmentSessions",
            "FingerprintScanEvents",
        ];

        private static readonly ForeignKeyConversion[] ForeignKeyConversions =
        [
            new("Students", "SectionId", "Sections", true),
            new("Sections", "CourseId", "Courses", true),
            new("Schedules", "SubjectId", "Subjects", true),
            new("Schedules", "ClassroomId", "Classrooms", true),
            new("Schedules", "SectionId", "Sections", true),
            new("Schedules", "InstructorId", "Instructors", true),
            new("StudentEnrollments", "StudentId", "Students", true),
            new("StudentEnrollments", "SectionId", "Sections", true),
            new("StudentEnrollments", "SubjectId", "Subjects", true),
            new("Sessions", "ScheduleId", "Schedules", true),
            new("Sessions", "ActualRoomId", "Classrooms", false),
            new("Sessions", "StartedBy", "Instructors", false),
            new("Sessions", "EndedBy", "Instructors", false),
            new("QrCodes", "SessionId", "Sessions", true),
            new("AttendanceRecords", "StudentId", "Students", true),
            new("AttendanceRecords", "SessionId", "Sessions", true),
            new("AttendanceRecords", "QrCodeId", "QrCodes", false),
            new("FingerprintScanEvents", "DeviceId", "FingerprintDevices", true),
            new("FingerprintScanEvents", "MatchedStudentId", "Students", false),
            new("FingerprintScanEvents", "SessionId", "Sessions", false),
            new("FingerprintScanEvents", "AttendanceRecordId", "AttendanceRecords", false),
            new("FingerprintEnrollmentSessions", "DeviceId", "FingerprintDevices", true),
            new("FingerprintEnrollmentSessions", "StudentId", "Students", true),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllUsers;");
            migrationBuilder.Sql(DropForeignKeysSql());
            migrationBuilder.Sql(DropAffectedIndexesSql());
            migrationBuilder.Sql(DropPrimaryKeysSql());

            foreach (var table in TargetTables)
            {
                migrationBuilder.Sql(PromoteUuidToPrimaryKeySql(table));
            }

            migrationBuilder.Sql(CreateLegacyIntIdIndexesSql());
            migrationBuilder.Sql(AddPrimaryKeysSql());

            foreach (var conversion in ForeignKeyConversions)
            {
                migrationBuilder.Sql(ConvertForeignKeySql(conversion));
            }

            migrationBuilder.Sql(CreateIndexesSql());
            migrationBuilder.Sql(CreateForeignKeysSql());

            migrationBuilder.Sql(CreateGetAllUsersProcedureSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!IsSqlServer(migrationBuilder))
            {
                return;
            }

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllUsers;");
            migrationBuilder.Sql(DropForeignKeysSql());
            migrationBuilder.Sql(DropAffectedIndexesSql());
            migrationBuilder.Sql(DropLegacyIntIdIndexesSql());
            migrationBuilder.Sql(DropPrimaryKeysSql());

            foreach (var conversion in ForeignKeyConversions)
            {
                migrationBuilder.Sql(RestoreForeignKeySql(conversion));
            }

            foreach (var table in TargetTables)
            {
                migrationBuilder.Sql(RestoreIntPrimaryKeySql(table));
            }

            migrationBuilder.Sql(CreateUuidIndexesSql());
            migrationBuilder.Sql(CreateIndexesSql());
            migrationBuilder.Sql(CreateForeignKeysSql());
            migrationBuilder.Sql(CreateLegacyGetAllUsersProcedureSql);
        }

        private static bool IsSqlServer(MigrationBuilder migrationBuilder)
            => migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

        private static string DropForeignKeysSql()
        {
            var convertedColumns = string.Join(", ", ForeignKeyConversions.Select(c => $"N'{c.ChildTable}.{c.Column}'"));
            var targetTables = string.Join(", ", TargetTables.Select(t => $"N'{t}'"));

            return $"""
                DECLARE @dropFkSql nvarchar(max) = N'';

                SELECT @dropFkSql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(parentTable.schema_id)) + N'.' + QUOTENAME(parentTable.name)
                    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
                FROM sys.foreign_keys fk
                JOIN sys.tables parentTable ON parentTable.object_id = fk.parent_object_id
                JOIN sys.tables referencedTable ON referencedTable.object_id = fk.referenced_object_id
                JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                JOIN sys.columns parentColumn ON parentColumn.object_id = fkc.parent_object_id AND parentColumn.column_id = fkc.parent_column_id
                WHERE CONCAT(parentTable.name, N'.', parentColumn.name) IN ({convertedColumns})
                    OR referencedTable.name IN ({targetTables});

                IF @dropFkSql <> N''
                BEGIN
                    EXEC sp_executesql @dropFkSql;
                END;
                """;
        }

        private static string DropAffectedIndexesSql()
        {
            var targetTables = string.Join(", ", TargetTables.Select(t => $"N'{t}'"));
            var convertedColumnNames = string.Join(", ", ForeignKeyConversions.Select(c => $"N'{c.Column}'").Distinct());

            return $"""
                DECLARE @dropIndexSql nvarchar(max) = N'';

                SELECT @dropIndexSql += N'DROP INDEX ' + QUOTENAME(indexes.name) + N' ON '
                    + QUOTENAME(SCHEMA_NAME(tables.schema_id)) + N'.' + QUOTENAME(tables.name) + N';' + CHAR(10)
                FROM sys.indexes indexes
                JOIN sys.tables tables ON tables.object_id = indexes.object_id
                WHERE tables.name IN ({targetTables})
                    AND indexes.is_primary_key = 0
                    AND indexes.is_unique_constraint = 0
                    AND EXISTS (
                        SELECT 1
                        FROM sys.index_columns indexColumns
                        JOIN sys.columns columns
                            ON columns.object_id = indexColumns.object_id
                            AND columns.column_id = indexColumns.column_id
                        WHERE indexColumns.object_id = indexes.object_id
                            AND indexColumns.index_id = indexes.index_id
                            AND (columns.name = N'Uuid' OR columns.name IN ({convertedColumnNames}))
                    );

                IF @dropIndexSql <> N''
                BEGIN
                    EXEC sp_executesql @dropIndexSql;
                END;
                """;
        }

        private static string DropPrimaryKeysSql()
        {
            var targetTables = string.Join(", ", TargetTables.Select(t => $"N'{t}'"));

            return $"""
                DECLARE @dropPkSql nvarchar(max) = N'';

                SELECT @dropPkSql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(tables.schema_id)) + N'.' + QUOTENAME(tables.name)
                    + N' DROP CONSTRAINT ' + QUOTENAME(keyConstraints.name) + N';' + CHAR(10)
                FROM sys.key_constraints keyConstraints
                JOIN sys.tables tables ON tables.object_id = keyConstraints.parent_object_id
                WHERE keyConstraints.[type] = 'PK'
                    AND tables.name IN ({targetTables});

                IF @dropPkSql <> N''
                BEGIN
                    EXEC sp_executesql @dropPkSql;
                END;
                """;
        }

        private static string PromoteUuidToPrimaryKeySql(string table)
        {
            var defaultConstraintVariable = $"@defaultConstraintName_{table}";

            return $"""
                IF COL_LENGTH(N'dbo.{table}', N'LegacyIntId') IS NULL
                BEGIN
                    EXEC sp_rename N'dbo.{table}.Id', N'LegacyIntId', N'COLUMN';
                END;

                IF COL_LENGTH(N'dbo.{table}', N'Id') IS NULL AND COL_LENGTH(N'dbo.{table}', N'Uuid') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'dbo.{table}.Uuid', N'Id', N'COLUMN';
                END;

                IF EXISTS (SELECT 1 FROM [dbo].[{table}] WHERE [Id] IS NULL)
                BEGIN
                    THROW 52000, '{table} contains NULL GUID Id values; cannot promote Uuid to primary key.', 1;
                END;

                IF EXISTS (
                    SELECT [Id]
                    FROM [dbo].[{table}]
                    GROUP BY [Id]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 52001, '{table} contains duplicate GUID Id values; cannot promote Uuid to primary key.', 1;
                END;

                DECLARE {defaultConstraintVariable} sysname;

                SELECT {defaultConstraintVariable} = defaultConstraints.name
                FROM sys.default_constraints defaultConstraints
                JOIN sys.columns columns
                    ON columns.object_id = defaultConstraints.parent_object_id
                    AND columns.column_id = defaultConstraints.parent_column_id
                WHERE defaultConstraints.parent_object_id = OBJECT_ID(N'dbo.{table}')
                    AND columns.name = N'Id';

                IF {defaultConstraintVariable} IS NOT NULL
                BEGIN
                    DECLARE @dropConstraintSql nvarchar(max) = N'ALTER TABLE [dbo].[{table}] DROP CONSTRAINT ' + QUOTENAME({defaultConstraintVariable}) + N';';
                    EXEC sp_executesql @dropConstraintSql;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.default_constraints defaultConstraints
                    JOIN sys.columns columns
                        ON columns.object_id = defaultConstraints.parent_object_id
                        AND columns.column_id = defaultConstraints.parent_column_id
                    WHERE defaultConstraints.parent_object_id = OBJECT_ID(N'dbo.{table}')
                        AND columns.name = N'Id'
                )
                BEGIN
                    ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [DF_{table}_Id] DEFAULT NEWSEQUENTIALID() FOR [Id];
                END;
                """;
        }

        private static string AddPrimaryKeysSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.key_constraints
                        WHERE [type] = 'PK'
                            AND parent_object_id = OBJECT_ID(N'dbo.{table}')
                    )
                    BEGIN
                        ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [PK_{table}] PRIMARY KEY ([Id]);
                    END;
                    """));

        private static string CreateLegacyIntIdIndexesSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_LegacyIntId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_{table}_LegacyIntId] ON [dbo].[{table}] ([LegacyIntId]);
                    END;
                    """));

        private static string DropLegacyIntIdIndexesSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_LegacyIntId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        DROP INDEX [IX_{table}_LegacyIntId] ON [dbo].[{table}];
                    END;
                    """));

        private static string ConvertForeignKeySql(ForeignKeyConversion conversion)
        {
            var tempColumn = $"{conversion.Column}_Guid";
            var nullability = conversion.Required ? "NOT NULL" : "NULL";
            var defaultConstraintVariable = $"@defaultConstraintName_{conversion.ChildTable}_{conversion.Column}";
            var missingPredicate = conversion.Required
                ? $"child.[{conversion.Column}] IS NULL OR parent.[Id] IS NULL"
                : $"child.[{conversion.Column}] IS NOT NULL AND parent.[Id] IS NULL";

            return $"""
                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{tempColumn}') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[{conversion.ChildTable}] ADD [{tempColumn}] uniqueidentifier NULL;
                END;

                DECLARE @backfillSql_{conversion.ChildTable}_{conversion.Column} nvarchar(max) = N'
                    UPDATE child
                    SET [{tempColumn}] = parent.[Id]
                    FROM [dbo].[{conversion.ChildTable}] child
                    LEFT JOIN [dbo].[{conversion.ParentTable}] parent
                        ON child.[{conversion.Column}] = parent.[LegacyIntId];';
                EXEC sp_executesql @backfillSql_{conversion.ChildTable}_{conversion.Column};

                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[{conversion.ChildTable}] child
                    LEFT JOIN [dbo].[{conversion.ParentTable}] parent
                        ON child.[{conversion.Column}] = parent.[LegacyIntId]
                    WHERE {missingPredicate}
                )
                BEGIN
                    THROW 53000, 'Could not backfill {conversion.ChildTable}.{conversion.Column} from {conversion.ParentTable}.LegacyIntId.', 1;
                END;

                DECLARE {defaultConstraintVariable} sysname;

                SELECT {defaultConstraintVariable} = defaultConstraints.name
                FROM sys.default_constraints defaultConstraints
                JOIN sys.columns columns
                    ON columns.object_id = defaultConstraints.parent_object_id
                    AND columns.column_id = defaultConstraints.parent_column_id
                WHERE defaultConstraints.parent_object_id = OBJECT_ID(N'dbo.{conversion.ChildTable}')
                    AND columns.name = N'{conversion.Column}';

                IF {defaultConstraintVariable} IS NOT NULL
                BEGIN
                    DECLARE @dropConstraintSql_{conversion.ChildTable}_{conversion.Column} nvarchar(max) = N'ALTER TABLE [dbo].[{conversion.ChildTable}] DROP CONSTRAINT ' + QUOTENAME({defaultConstraintVariable}) + N';';
                    EXEC sp_executesql @dropConstraintSql_{conversion.ChildTable}_{conversion.Column};
                END;

                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{conversion.Column}') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[{conversion.ChildTable}] DROP COLUMN [{conversion.Column}];
                END;

                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{conversion.Column}') IS NULL
                    AND COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{tempColumn}') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'dbo.{conversion.ChildTable}.{tempColumn}', N'{conversion.Column}', N'COLUMN';
                END;

                ALTER TABLE [dbo].[{conversion.ChildTable}] ALTER COLUMN [{conversion.Column}] uniqueidentifier {nullability};
                """;
        }

        private static string RestoreForeignKeySql(ForeignKeyConversion conversion)
        {
            var tempColumn = $"{conversion.Column}_LegacyInt";
            var nullability = conversion.Required ? "NOT NULL" : "NULL";
            var missingPredicate = conversion.Required
                ? $"child.[{conversion.Column}] IS NULL OR parent.[LegacyIntId] IS NULL"
                : $"child.[{conversion.Column}] IS NOT NULL AND parent.[LegacyIntId] IS NULL";

            return $"""
                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{tempColumn}') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[{conversion.ChildTable}] ADD [{tempColumn}] int NULL;
                END;

                DECLARE @restoreBackfillSql_{conversion.ChildTable}_{conversion.Column} nvarchar(max) = N'
                    UPDATE child
                    SET [{tempColumn}] = parent.[LegacyIntId]
                    FROM [dbo].[{conversion.ChildTable}] child
                    LEFT JOIN [dbo].[{conversion.ParentTable}] parent
                        ON child.[{conversion.Column}] = parent.[Id];';
                EXEC sp_executesql @restoreBackfillSql_{conversion.ChildTable}_{conversion.Column};

                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[{conversion.ChildTable}] child
                    LEFT JOIN [dbo].[{conversion.ParentTable}] parent
                        ON child.[{conversion.Column}] = parent.[Id]
                    WHERE {missingPredicate}
                )
                BEGIN
                    THROW 54000, 'Could not restore {conversion.ChildTable}.{conversion.Column} from {conversion.ParentTable}.LegacyIntId.', 1;
                END;

                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{conversion.Column}') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[{conversion.ChildTable}] DROP COLUMN [{conversion.Column}];
                END;

                IF COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{conversion.Column}') IS NULL
                    AND COL_LENGTH(N'dbo.{conversion.ChildTable}', N'{tempColumn}') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'dbo.{conversion.ChildTable}.{tempColumn}', N'{conversion.Column}', N'COLUMN';
                END;

                ALTER TABLE [dbo].[{conversion.ChildTable}] ALTER COLUMN [{conversion.Column}] int {nullability};
                """;
        }

        private static string RestoreIntPrimaryKeySql(string table)
        {
            var defaultConstraintVariable = $"@defaultConstraintName_Down_{table}";

            return $"""
                DECLARE {defaultConstraintVariable} sysname;

                SELECT {defaultConstraintVariable} = defaultConstraints.name
                FROM sys.default_constraints defaultConstraints
                JOIN sys.columns columns
                    ON columns.object_id = defaultConstraints.parent_object_id
                    AND columns.column_id = defaultConstraints.parent_column_id
                WHERE defaultConstraints.parent_object_id = OBJECT_ID(N'dbo.{table}')
                    AND columns.name = N'Id';

                IF {defaultConstraintVariable} IS NOT NULL
                BEGIN
                    DECLARE @dropConstraintSql_Down_{table} nvarchar(max) = N'ALTER TABLE [dbo].[{table}] DROP CONSTRAINT ' + QUOTENAME({defaultConstraintVariable}) + N';';
                    EXEC sp_executesql @dropConstraintSql_Down_{table};
                END;

                IF COL_LENGTH(N'dbo.{table}', N'Uuid') IS NULL AND COL_LENGTH(N'dbo.{table}', N'Id') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'dbo.{table}.Id', N'Uuid', N'COLUMN';
                END;

                IF COL_LENGTH(N'dbo.{table}', N'Id') IS NULL AND COL_LENGTH(N'dbo.{table}', N'LegacyIntId') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'dbo.{table}.LegacyIntId', N'Id', N'COLUMN';
                END;

                IF EXISTS (SELECT 1 FROM [dbo].[{table}] WHERE [Id] IS NULL)
                BEGIN
                    THROW 55000, '{table} contains NULL legacy integer Id values; cannot restore integer primary key.', 1;
                END;

                IF EXISTS (
                    SELECT [Id]
                    FROM [dbo].[{table}]
                    GROUP BY [Id]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 55001, '{table} contains duplicate legacy integer Id values; cannot restore integer primary key.', 1;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.key_constraints
                    WHERE [type] = 'PK'
                        AND parent_object_id = OBJECT_ID(N'dbo.{table}')
                )
                BEGIN
                    ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [PK_{table}] PRIMARY KEY ([Id]);
                END;
                """;
        }

        private static string CreateIndexesSql()
            => """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Admins_UserId' AND object_id = OBJECT_ID(N'dbo.Admins'))
                    CREATE UNIQUE INDEX [IX_Admins_UserId] ON [dbo].[Admins] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_CheckInTime' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE INDEX [IX_AttendanceRecords_CheckInTime] ON [dbo].[AttendanceRecords] ([CheckInTime]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_QrCodeId' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE INDEX [IX_AttendanceRecords_QrCodeId] ON [dbo].[AttendanceRecords] ([QrCodeId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_SessionId' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE INDEX [IX_AttendanceRecords_SessionId] ON [dbo].[AttendanceRecords] ([SessionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_Status' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE INDEX [IX_AttendanceRecords_Status] ON [dbo].[AttendanceRecords] ([Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_StudentId' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE INDEX [IX_AttendanceRecords_StudentId] ON [dbo].[AttendanceRecords] ([StudentId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_StudentId_SessionId' AND object_id = OBJECT_ID(N'dbo.AttendanceRecords'))
                    CREATE UNIQUE INDEX [IX_AttendanceRecords_StudentId_SessionId] ON [dbo].[AttendanceRecords] ([StudentId], [SessionId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Classrooms_Name' AND object_id = OBJECT_ID(N'dbo.Classrooms'))
                    CREATE UNIQUE INDEX [IX_Classrooms_Name] ON [dbo].[Classrooms] ([Name]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Courses_Name' AND object_id = OBJECT_ID(N'dbo.Courses'))
                    CREATE UNIQUE INDEX [IX_Courses_Name] ON [dbo].[Courses] ([Name]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Fingerprints_UserId_Active' AND object_id = OBJECT_ID(N'dbo.Fingerprints'))
                    CREATE UNIQUE INDEX [IX_Fingerprints_UserId_Active] ON [dbo].[Fingerprints] ([UserId]) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Fingerprints_DeviceId_SensorFingerprintId_Active' AND object_id = OBJECT_ID(N'dbo.Fingerprints'))
                    CREATE UNIQUE INDEX [IX_Fingerprints_DeviceId_SensorFingerprintId_Active] ON [dbo].[Fingerprints] ([DeviceId], [SensorFingerprintId]) WHERE [IsDeleted] = 0;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintDevices_DeviceIdentifier' AND object_id = OBJECT_ID(N'dbo.FingerprintDevices'))
                    CREATE UNIQUE INDEX [IX_FingerprintDevices_DeviceIdentifier] ON [dbo].[FingerprintDevices] ([DeviceIdentifier]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintDevices_IsActive' AND object_id = OBJECT_ID(N'dbo.FingerprintDevices'))
                    CREATE INDEX [IX_FingerprintDevices_IsActive] ON [dbo].[FingerprintDevices] ([IsActive]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintEnrollmentSessions_EnrollmentSessionId' AND object_id = OBJECT_ID(N'dbo.FingerprintEnrollmentSessions'))
                    CREATE UNIQUE INDEX [IX_FingerprintEnrollmentSessions_EnrollmentSessionId] ON [dbo].[FingerprintEnrollmentSessions] ([EnrollmentSessionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintEnrollmentSessions_DeviceId_Status' AND object_id = OBJECT_ID(N'dbo.FingerprintEnrollmentSessions'))
                    CREATE INDEX [IX_FingerprintEnrollmentSessions_DeviceId_Status] ON [dbo].[FingerprintEnrollmentSessions] ([DeviceId], [Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintEnrollmentSessions_StudentId_Status' AND object_id = OBJECT_ID(N'dbo.FingerprintEnrollmentSessions'))
                    CREATE INDEX [IX_FingerprintEnrollmentSessions_StudentId_Status] ON [dbo].[FingerprintEnrollmentSessions] ([StudentId], [Status]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_AttendanceRecordId' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE UNIQUE INDEX [IX_FingerprintScanEvents_AttendanceRecordId] ON [dbo].[FingerprintScanEvents] ([AttendanceRecordId]) WHERE [AttendanceRecordId] IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_EventId' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE UNIQUE INDEX [IX_FingerprintScanEvents_EventId] ON [dbo].[FingerprintScanEvents] ([EventId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_SessionId' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE INDEX [IX_FingerprintScanEvents_SessionId] ON [dbo].[FingerprintScanEvents] ([SessionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_Status' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE INDEX [IX_FingerprintScanEvents_Status] ON [dbo].[FingerprintScanEvents] ([Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_DeviceId_CapturedAt' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE INDEX [IX_FingerprintScanEvents_DeviceId_CapturedAt] ON [dbo].[FingerprintScanEvents] ([DeviceId], [CapturedAt]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FingerprintScanEvents_MatchedStudentId_CapturedAt' AND object_id = OBJECT_ID(N'dbo.FingerprintScanEvents'))
                    CREATE INDEX [IX_FingerprintScanEvents_MatchedStudentId_CapturedAt] ON [dbo].[FingerprintScanEvents] ([MatchedStudentId], [CapturedAt]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Instructors_UserId' AND object_id = OBJECT_ID(N'dbo.Instructors'))
                    CREATE UNIQUE INDEX [IX_Instructors_UserId] ON [dbo].[Instructors] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QrCodes_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.QrCodes'))
                    CREATE INDEX [IX_QrCodes_ExpiresAt] ON [dbo].[QrCodes] ([ExpiresAt]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QrCodes_IsActive' AND object_id = OBJECT_ID(N'dbo.QrCodes'))
                    CREATE INDEX [IX_QrCodes_IsActive] ON [dbo].[QrCodes] ([IsActive]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QrCodes_QrHash' AND object_id = OBJECT_ID(N'dbo.QrCodes'))
                    CREATE UNIQUE INDEX [IX_QrCodes_QrHash] ON [dbo].[QrCodes] ([QrHash]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QrCodes_SessionId' AND object_id = OBJECT_ID(N'dbo.QrCodes'))
                    CREATE INDEX [IX_QrCodes_SessionId] ON [dbo].[QrCodes] ([SessionId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_ClassroomId' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_ClassroomId] ON [dbo].[Schedules] ([ClassroomId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_DayOfWeek' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_DayOfWeek] ON [dbo].[Schedules] ([DayOfWeek]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_InstructorId' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_InstructorId] ON [dbo].[Schedules] ([InstructorId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_SectionId' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_SectionId] ON [dbo].[Schedules] ([SectionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_SubjectId' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_SubjectId] ON [dbo].[Schedules] ([SubjectId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_TimeIn' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_TimeIn] ON [dbo].[Schedules] ([TimeIn]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_TimeOut' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE INDEX [IX_Schedules_TimeOut] ON [dbo].[Schedules] ([TimeOut]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Schedules_TimeIn_TimeOut' AND object_id = OBJECT_ID(N'dbo.Schedules'))
                    CREATE UNIQUE INDEX [IX_Schedules_TimeIn_TimeOut] ON [dbo].[Schedules] ([TimeIn], [TimeOut]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sections_CourseId' AND object_id = OBJECT_ID(N'dbo.Sections'))
                    CREATE INDEX [IX_Sections_CourseId] ON [dbo].[Sections] ([CourseId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sections_Name' AND object_id = OBJECT_ID(N'dbo.Sections'))
                    CREATE UNIQUE INDEX [IX_Sections_Name] ON [dbo].[Sections] ([Name]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_ActualRoomId' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_ActualRoomId] ON [dbo].[Sessions] ([ActualRoomId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_EndedBy' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_EndedBy] ON [dbo].[Sessions] ([EndedBy]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_ScheduleId' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_ScheduleId] ON [dbo].[Sessions] ([ScheduleId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_SessionDate' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_SessionDate] ON [dbo].[Sessions] ([SessionDate]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_StartedBy' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_StartedBy] ON [dbo].[Sessions] ([StartedBy]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_Status' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_Status] ON [dbo].[Sessions] ([Status]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sessions_ScheduleId_SessionDate' AND object_id = OBJECT_ID(N'dbo.Sessions'))
                    CREATE INDEX [IX_Sessions_ScheduleId_SessionDate] ON [dbo].[Sessions] ([ScheduleId], [SessionDate]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_SectionId' AND object_id = OBJECT_ID(N'dbo.Students'))
                    CREATE INDEX [IX_Students_SectionId] ON [dbo].[Students] ([SectionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_UserId' AND object_id = OBJECT_ID(N'dbo.Students'))
                    CREATE UNIQUE INDEX [IX_Students_UserId] ON [dbo].[Students] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentEnrollments_IsActive' AND object_id = OBJECT_ID(N'dbo.StudentEnrollments'))
                    CREATE INDEX [IX_StudentEnrollments_IsActive] ON [dbo].[StudentEnrollments] ([IsActive]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentEnrollments_SectionId' AND object_id = OBJECT_ID(N'dbo.StudentEnrollments'))
                    CREATE INDEX [IX_StudentEnrollments_SectionId] ON [dbo].[StudentEnrollments] ([SectionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentEnrollments_StudentId' AND object_id = OBJECT_ID(N'dbo.StudentEnrollments'))
                    CREATE INDEX [IX_StudentEnrollments_StudentId] ON [dbo].[StudentEnrollments] ([StudentId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentEnrollments_SubjectId' AND object_id = OBJECT_ID(N'dbo.StudentEnrollments'))
                    CREATE INDEX [IX_StudentEnrollments_SubjectId] ON [dbo].[StudentEnrollments] ([SubjectId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentEnrollments_StudentId_SectionId_SubjectId' AND object_id = OBJECT_ID(N'dbo.StudentEnrollments'))
                    CREATE UNIQUE INDEX [IX_StudentEnrollments_StudentId_SectionId_SubjectId] ON [dbo].[StudentEnrollments] ([StudentId], [SectionId], [SubjectId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subjects_Code' AND object_id = OBJECT_ID(N'dbo.Subjects'))
                    CREATE UNIQUE INDEX [IX_Subjects_Code] ON [dbo].[Subjects] ([Code]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subjects_Name' AND object_id = OBJECT_ID(N'dbo.Subjects'))
                    CREATE UNIQUE INDEX [IX_Subjects_Name] ON [dbo].[Subjects] ([Name]);
                """;

        private static string CreateForeignKeysSql()
            => """
                ALTER TABLE [dbo].[AttendanceRecords] ADD CONSTRAINT [FK_AttendanceRecords_QrCodes_QrCodeId] FOREIGN KEY ([QrCodeId]) REFERENCES [dbo].[QrCodes] ([Id]) ON DELETE SET NULL;
                ALTER TABLE [dbo].[AttendanceRecords] ADD CONSTRAINT [FK_AttendanceRecords_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Sessions] ([Id]) ON DELETE CASCADE;
                ALTER TABLE [dbo].[AttendanceRecords] ADD CONSTRAINT [FK_AttendanceRecords_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[FingerprintEnrollmentSessions] ADD CONSTRAINT [FK_FingerprintEnrollmentSessions_FingerprintDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[FingerprintDevices] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[FingerprintEnrollmentSessions] ADD CONSTRAINT [FK_FingerprintEnrollmentSessions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[FingerprintScanEvents] ADD CONSTRAINT [FK_FingerprintScanEvents_AttendanceRecords_AttendanceRecordId] FOREIGN KEY ([AttendanceRecordId]) REFERENCES [dbo].[AttendanceRecords] ([Id]) ON DELETE SET NULL;
                ALTER TABLE [dbo].[FingerprintScanEvents] ADD CONSTRAINT [FK_FingerprintScanEvents_FingerprintDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[FingerprintDevices] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[FingerprintScanEvents] ADD CONSTRAINT [FK_FingerprintScanEvents_Students_MatchedStudentId] FOREIGN KEY ([MatchedStudentId]) REFERENCES [dbo].[Students] ([Id]) ON DELETE SET NULL;
                ALTER TABLE [dbo].[FingerprintScanEvents] ADD CONSTRAINT [FK_FingerprintScanEvents_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Sessions] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[QrCodes] ADD CONSTRAINT [FK_QrCodes_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Sessions] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Schedules] ADD CONSTRAINT [FK_Schedules_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Schedules] ADD CONSTRAINT [FK_Schedules_Instructors_InstructorId] FOREIGN KEY ([InstructorId]) REFERENCES [dbo].[Instructors] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Schedules] ADD CONSTRAINT [FK_Schedules_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[Sections] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Schedules] ADD CONSTRAINT [FK_Schedules_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Sections] ADD CONSTRAINT [FK_Sections_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses] ([Id]) ON DELETE CASCADE;
                ALTER TABLE [dbo].[Sessions] ADD CONSTRAINT [FK_Sessions_Classrooms_ActualRoomId] FOREIGN KEY ([ActualRoomId]) REFERENCES [dbo].[Classrooms] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Sessions] ADD CONSTRAINT [FK_Sessions_Instructors_EndedBy] FOREIGN KEY ([EndedBy]) REFERENCES [dbo].[Instructors] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Sessions] ADD CONSTRAINT [FK_Sessions_Schedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [dbo].[Schedules] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Sessions] ADD CONSTRAINT [FK_Sessions_Instructors_StartedBy] FOREIGN KEY ([StartedBy]) REFERENCES [dbo].[Instructors] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[Students] ADD CONSTRAINT [FK_Students_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[Sections] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[StudentEnrollments] ADD CONSTRAINT [FK_StudentEnrollments_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [dbo].[Sections] ([Id]) ON DELETE NO ACTION;
                ALTER TABLE [dbo].[StudentEnrollments] ADD CONSTRAINT [FK_StudentEnrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([Id]) ON DELETE CASCADE;
                ALTER TABLE [dbo].[StudentEnrollments] ADD CONSTRAINT [FK_StudentEnrollments_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([Id]) ON DELETE NO ACTION;
                """;

        private static string CreateUuidIndexesSql()
            => string.Join(
                Environment.NewLine,
                TargetTables.Select(table =>
                    $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_Uuid' AND object_id = OBJECT_ID(N'dbo.{table}'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_{table}_Uuid] ON [dbo].[{table}] ([Uuid]);
                    END;
                    """));

        private const string CreateGetAllUsersProcedureSql = """
            EXEC(N'
            CREATE PROCEDURE sp_GetAllUsers
                @Status INT = 0
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    u.Id AS UserId,
                    u.UserName AS Username,
                    u.Email,
                    ISNULL(r.Name, ''Unknown'') AS Role,
                    COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                    COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                    COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                    i.Department AS Department,
                    COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                    COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt,
                    CAST(
                        CASE
                            WHEN COALESCE(
                                CAST(s.IsDeleted AS INT),
                                CAST(i.IsDeleted AS INT),
                                CAST(a.IsDeleted AS INT),
                                0
                            ) = 1 THEN 1
                            ELSE 0
                        END
                    AS BIT) AS IsDeleted,
                    COALESCE(s.DeletedAt, i.DeletedAt, a.DeletedAt) AS DeletedAt,
                    s.SectionId AS SectionId,
                    sec.Name AS SectionName,
                    sec.CourseId AS CourseId,
                    c.Name AS CourseName,
                    s.IsRegular AS IsRegular
                FROM AspNetUsers u
                LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                LEFT JOIN Students s ON u.Id = s.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND s.IsDeleted = 0) OR
                        (@Status = 1 AND s.IsDeleted = 1)
                    )
                LEFT JOIN Sections sec ON s.SectionId = sec.Id
                LEFT JOIN Courses c ON sec.CourseId = c.Id
                LEFT JOIN Instructors i ON u.Id = i.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND i.IsDeleted = 0) OR
                        (@Status = 1 AND i.IsDeleted = 1)
                    )
                LEFT JOIN Admins a ON u.Id = a.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)) OR
                        (@Status = 1 AND a.IsDeleted = 1)
                    )
                WHERE
                    @Status = 2 OR
                    (s.Id IS NOT NULL OR i.Id IS NOT NULL OR a.Id IS NOT NULL)
                ORDER BY u.UserName;
            END
            ');
            """;

        private const string CreateLegacyGetAllUsersProcedureSql = """
            EXEC(N'
            CREATE PROCEDURE sp_GetAllUsers
                @Status INT = 0
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    u.Id AS UserId,
                    u.UserName AS Username,
                    u.Email,
                    ISNULL(r.Name, ''Unknown'') AS Role,
                    COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                    COALESCE(s.Uuid, i.Uuid, a.Uuid) AS ProfileUuid,
                    COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                    COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                    i.Department AS Department,
                    COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                    COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt,
                    CAST(
                        CASE
                            WHEN COALESCE(
                                CAST(s.IsDeleted AS INT),
                                CAST(i.IsDeleted AS INT),
                                CAST(a.IsDeleted AS INT),
                                0
                            ) = 1 THEN 1
                            ELSE 0
                        END
                    AS BIT) AS IsDeleted,
                    COALESCE(s.DeletedAt, i.DeletedAt, a.DeletedAt) AS DeletedAt,
                    s.SectionId AS SectionId,
                    sec.Uuid AS SectionUuid,
                    sec.Name AS SectionName,
                    sec.CourseId AS CourseId,
                    c.Uuid AS CourseUuid,
                    c.Name AS CourseName,
                    s.IsRegular AS IsRegular
                FROM AspNetUsers u
                LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                LEFT JOIN Students s ON u.Id = s.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND s.IsDeleted = 0) OR
                        (@Status = 1 AND s.IsDeleted = 1)
                    )
                LEFT JOIN Sections sec ON s.SectionId = sec.Id
                LEFT JOIN Courses c ON sec.CourseId = c.Id
                LEFT JOIN Instructors i ON u.Id = i.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND i.IsDeleted = 0) OR
                        (@Status = 1 AND i.IsDeleted = 1)
                    )
                LEFT JOIN Admins a ON u.Id = a.UserId
                    AND (
                        @Status = 2 OR
                        (@Status = 0 AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)) OR
                        (@Status = 1 AND a.IsDeleted = 1)
                    )
                WHERE
                    @Status = 2 OR
                    (s.Id IS NOT NULL OR i.Id IS NOT NULL OR a.Id IS NOT NULL)
                ORDER BY u.UserName;
            END
            ');
            """;

        private readonly record struct ForeignKeyConversion(
            string ChildTable,
            string Column,
            string ParentTable,
            bool Required);
    }
}
