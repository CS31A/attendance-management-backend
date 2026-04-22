using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Data;
using System.Data.Common;

namespace attendance.testproject.Repositories_Testing;

public class AccountRepositoryTransactionTests
{
    [Fact]
    public async Task DeleteUserAsyncSP_DoesNotCloseConnection_WhenConnectionWasAlreadyOpen()
    {
        await using var innerConnection = new SqliteConnection("Data Source=:memory:");
        await innerConnection.OpenAsync();

        var setupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(innerConnection)
            .Options;
        await using (var setupContext = new ApplicationDbContext(setupOptions))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var connection = new TrackingDbConnection(innerConnection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);

        var repository = new AccountRepository(null!, null!, null!, context);

        await context.Database.OpenConnectionAsync();
        Assert.Equal(System.Data.ConnectionState.Open, context.Database.GetDbConnection().State);

        await Assert.ThrowsAnyAsync<Exception>(() => repository.DeleteUserAsyncSP("user-1"));

        Assert.Equal(0, connection.CloseCallCount);
    }

    [Fact]
    public async Task DeleteUserAsyncSP_KeepsConnectionOpen_WhenEfTransactionIsActive()
    {
        await using var innerConnection = new SqliteConnection("Data Source=:memory:");
        await innerConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(innerConnection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new AccountRepository(null!, null!, null!, context);

        await using var transaction = await context.Database.BeginTransactionAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => repository.DeleteUserAsyncSP("user-1"));

        Assert.Equal(System.Data.ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task GetAllUsersAsyncSP_AssignsCurrentTransaction_BeforeProviderSpecificFailure()
    {
        await using var innerConnection = new SqliteConnection("Data Source=:memory:");
        await innerConnection.OpenAsync();

        var setupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(innerConnection)
            .Options;
        await using (var setupContext = new ApplicationDbContext(setupOptions))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var connection = new TrackingDbConnection(innerConnection);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);

        var repository = new AccountRepository(null!, null!, null!, context);

        await using var transaction = await context.Database.BeginTransactionAsync();

        // SQLite cannot execute the SQL Server stored procedure path, but the command
        // should still be enlisted in the active EF transaction before the provider fails.
        await Assert.ThrowsAnyAsync<Exception>(() => repository.GetAllUsersAsyncSP(UserStatus.All));

        Assert.NotNull(connection.LastAssignedTransaction);
        Assert.Same(transaction.GetDbTransaction(), connection.LastAssignedTransaction);
    }

    [Fact]
    public async Task GetAdminByUserIdAsync_AndGetAdminByUuidAsync_ReturnPersistedAdmin()
    {
        await using var innerConnection = new SqliteConnection("Data Source=:memory:");
        await innerConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(innerConnection)
            .Options;

        var adminUuid = Guid.NewGuid();

        await using (var setupContext = new ApplicationDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.Users.Add(new IdentityUser
            {
                Id = "admin-user",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@test.com",
                NormalizedEmail = "ADMIN@TEST.COM"
            });
            setupContext.Admins.Add(new attendance_monitoring.Classes.Admin
            {
                UserId = "admin-user",
                Uuid = adminUuid,
                Firstname = "Ada",
                Lastname = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await setupContext.SaveChangesAsync();
        }

        await using var context = new ApplicationDbContext(options);
        var repository = new AccountRepository(null!, null!, null!, context);

        var byUserId = await repository.GetAdminByUserIdAsync("admin-user");
        var byUuid = await repository.GetAdminByUuidAsync(adminUuid);

        Assert.NotNull(byUserId);
        Assert.NotNull(byUuid);
        Assert.Equal(adminUuid, byUserId!.Uuid);
        Assert.Equal("admin-user", byUuid!.UserId);
    }

    private sealed class TrackingDbConnection : DbConnection
    {
        private readonly DbConnection _inner;

        public TrackingDbConnection(DbConnection inner)
        {
            _inner = inner;
        }

        public int CloseCallCount { get; private set; }

        public DbTransaction? LastAssignedTransaction { get; private set; }

        [AllowNull]
        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        public override string Database => _inner.Database;

        public override string DataSource => _inner.DataSource;

        public override string ServerVersion => _inner.ServerVersion;

        public override System.Data.ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        public override void Close()
        {
            CloseCallCount++;
            _inner.Close();
        }

        public override void Open() => _inner.Open();

        public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
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

            public override ValueTask DisposeAsync()
            {
                return _inner.DisposeAsync();
            }
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
                set => _inner.CommandText = value;
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

            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
                => _inner.ExecuteReaderAsync(behavior, cancellationToken);
        }
    }
}
