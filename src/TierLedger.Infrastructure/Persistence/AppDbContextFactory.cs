using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TierLedger.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (dotnet ef). Migrations are
/// always generated against SQL Server — the production provider — regardless
/// of the provider the app happens to run with locally.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=TierLedger;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
