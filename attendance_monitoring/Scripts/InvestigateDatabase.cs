using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetEnv;

namespace DatabaseInvestigation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("ERROR: Connection string not found");
                return;
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                Console.WriteLine("✓ Connected to database successfully\n");

                // Check QrCodes table constraints
                Console.WriteLine("=== QrCodes Foreign Key Constraints ===");
                var constraintQuery = @"
                    SELECT 
                        OBJECT_NAME(parent_object_id) AS TableName,
                        name AS ConstraintName,
                        type_desc AS ConstraintType
                    FROM sys.foreign_keys 
                    WHERE parent_object_id = OBJECT_ID('QrCodes');";

                using (var cmd = new SqlCommand(constraintQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"Table: {reader["TableName"]}, Constraint: {reader["ConstraintName"]}, Type: {reader["ConstraintType"]}");
                    }
                }

                Console.WriteLine("\n=== Migration History ===");
                var migrationQuery = @"
                    SELECT MigrationId, ProductVersion 
                    FROM __EFMigrationsHistory 
                    ORDER BY MigrationId;";

                using (var cmd = new SqlCommand(migrationQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"{reader["MigrationId"]} - {reader["ProductVersion"]}");
                    }
                }

                Console.WriteLine("\n=== QrCodes Table Structure ===");
                var tableQuery = @"
                    SELECT 
                        c.name AS ColumnName,
                        t.name AS DataType,
                        c.is_nullable AS IsNullable,
                        c.max_length AS MaxLength
                    FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID('QrCodes')
                    ORDER BY c.column_id;";

                using (var cmd = new SqlCommand(tableQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Console.WriteLine("Column Name | Data Type | Nullable | Max Length");
                    Console.WriteLine("----------------------------------------------");
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"{reader["ColumnName"]} | {reader["DataType"]} | {reader["IsNullable"]} | {reader["MaxLength"]}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
