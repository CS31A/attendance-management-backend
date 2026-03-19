using attendance.testproject;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
namespace attendance.testproject.Integration_Testing;

public class AccountRepositorySqlServerTransactionIntegrationTests
{
    [RequiresEnvironmentVariableFact("ATTENDANCE_SQLSERVER_TEST_CONNECTION_STRING")]
    public async Task GetAllUsersAsyncSP_UsesCurrentTransaction_OnSqlServer()
    {
        var connectionString = GetSqlServerConnectionString()!;

        await using var innerConnection = new SqlConnection(connectionString);
        await using var connection = new TrackingDbConnection(innerConnection);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        var repository = new AccountRepository(null!, null!, null!, context);

        await using var transaction = await context.Database.BeginTransactionAsync();

        var users = await repository.GetAllUsersAsyncSP(UserStatus.All);

        Assert.NotNull(users);
        Assert.NotNull(connection.LastAssignedTransaction);
        Assert.Same(transaction.GetDbTransaction(), connection.LastAssignedTransaction);
        Assert.Contains("sp_GetAllUsers", connection.LastCommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    private static string? GetSqlServerConnectionString()
    {
        return Environment.GetEnvironmentVariable("ATTENDANCE_SQLSERVER_TEST_CONNECTION_STRING");
    }

    private sealed class TrackingDbConnection : DbConnection
    {
        private readonly DbConnection _inner;

        public TrackingDbConnection(DbConnection inner)
        {
            _inner = inner;
        }

        public DbTransaction? LastAssignedTransaction { get; private set; }

        public string LastCommandText { get; private set; } = string.Empty;

        [AllowNull]
        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        public override string Database => _inner.Database;

        public override string DataSource => _inner.DataSource;

        public override string ServerVersion => _inner.ServerVersion;

        public override ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        public override void Close() => _inner.Close();

        public override void Open() => _inner.Open();

        public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new TrackingDbTransaction(_inner.BeginTransaction(isolationLevel), this);

        protected override DbCommand CreateDbCommand() => new TrackingDbCommand(_inner.CreateCommand(), this);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            await base.DisposeAsync();
        }

        private sealed class TrackingDbTransaction : DbTransaction
        {
            private readonly DbTransaction _inner;
            private readonly TrackingDbConnection _owner;

            public TrackingDbTransaction(DbTransaction inner, TrackingDbConnection owner)
            {
                _inner = inner;
                _owner = owner;
            }

            public DbTransaction InnerTransaction => _inner;

            public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

            protected override DbConnection DbConnection => _owner;

            public override void Commit() => _inner.Commit();

            public override void Rollback() => _inner.Rollback();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }

            public override ValueTask DisposeAsync() => _inner.DisposeAsync();
        }

        private sealed class TrackingDbCommand : DbCommand
        {
            private readonly DbCommand _inner;
            private readonly TrackingDbConnection _owner;

            public TrackingDbCommand(DbCommand inner, TrackingDbConnection owner)
            {
                _inner = inner;
                _owner = owner;
            }

            [AllowNull]
            public override string CommandText
            {
                get => _inner.CommandText;
                set
                {
                    _owner.LastCommandText = value ?? string.Empty;
                    _inner.CommandText = value;
                }
            }

            public override int CommandTimeout
            {
                get => _inner.CommandTimeout;
                set => _inner.CommandTimeout = value;
            }

            public override CommandType CommandType
            {
                get => _inner.CommandType;
                set => _inner.CommandType = value;
            }

            protected override DbConnection? DbConnection
            {
                get => _inner.Connection;
                set => _inner.Connection = value;
            }

            protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

            protected override DbTransaction? DbTransaction
            {
                get => _inner.Transaction;
                set
                {
                    _owner.LastAssignedTransaction = value;
                    _inner.Transaction = value is TrackingDbTransaction trackingTransaction
                        ? trackingTransaction.InnerTransaction
                        : value;
                }
            }

            public override bool DesignTimeVisible
            {
                get => _inner.DesignTimeVisible;
                set => _inner.DesignTimeVisible = value;
            }

            public override UpdateRowSource UpdatedRowSource
            {
                get => _inner.UpdatedRowSource;
                set => _inner.UpdatedRowSource = value;
            }

            public override void Cancel() => _inner.Cancel();

            public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();

            public override object? ExecuteScalar() => _inner.ExecuteScalar();

            public override void Prepare() => _inner.Prepare();

            protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                => _inner.ExecuteReader(behavior);

            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
                => _inner.ExecuteNonQueryAsync(cancellationToken);

            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
                => _inner.ExecuteScalarAsync(cancellationToken);

            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
                CommandBehavior behavior,
                CancellationToken cancellationToken)
                => _inner.ExecuteReaderAsync(behavior, cancellationToken);
        }
    }
}
