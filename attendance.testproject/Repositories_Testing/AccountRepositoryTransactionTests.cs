using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
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

        await repository.DeleteUserAsyncSP("user-1");

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

        await repository.DeleteUserAsyncSP("user-1");

        Assert.Equal(System.Data.ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    private sealed class TrackingDbConnection : DbConnection
    {
        private readonly DbConnection _inner;

        public TrackingDbConnection(DbConnection inner)
        {
            _inner = inner;
        }

        public int CloseCallCount { get; private set; }

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
            => _inner.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand() => _inner.CreateCommand();

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
    }
}
