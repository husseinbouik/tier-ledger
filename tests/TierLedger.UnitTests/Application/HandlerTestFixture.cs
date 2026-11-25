using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TierLedger.Application;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Infrastructure.Persistence;

namespace TierLedger.UnitTests.Application;

/// <summary>
/// Wires the real MediatR pipeline (logging + validation behaviours included)
/// and the real EF Core DbContext against a private in-memory SQLite database,
/// so handler tests exercise the same code paths as the API — minus HTTP and
/// minus the outbox dispatcher (events land in the outbox table; process
/// manager tests invoke the handlers directly, integration tests cover the
/// full dispatch loop).
/// </summary>
public sealed class HandlerTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public HandlerTestFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(Context);
        services.AddSingleton<IInvoiceNumberGenerator>(new InvoiceNumberGenerator(Context));

        Provider = services.BuildServiceProvider();
        Mediator = Provider.GetRequiredService<IMediator>();
    }

    public AppDbContext Context { get; }
    public ServiceProvider Provider { get; }
    public IMediator Mediator { get; }

    public void Dispose()
    {
        Provider.Dispose();
        Context.Dispose();
        _connection.Dispose();
    }
}
