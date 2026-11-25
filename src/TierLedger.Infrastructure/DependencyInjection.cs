using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Infrastructure.Outbox;
using TierLedger.Infrastructure.Persistence;
using TierLedger.Infrastructure.Processes;

namespace TierLedger.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and background services. The EF Core provider is
    /// chosen by configuration ("Database:Provider" = SqlServer | Sqlite):
    /// SQL Server is the production target (see docker-compose), SQLite keeps
    /// local development and the test suites free of external dependencies.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=tierledger.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(connectionString);
            else
                options.UseSqlite(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<OverdueScanOptions>(configuration.GetSection(OverdueScanOptions.SectionName));
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<OverdueInvoiceScanner>();

        return services;
    }
}
