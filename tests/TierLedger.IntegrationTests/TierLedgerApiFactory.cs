using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TierLedger.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway SQLite file, with the outbox
/// dispatcher polling every second and the overdue scanner disabled, so the
/// full request → aggregate → outbox → process-manager loop can be asserted
/// over HTTP.
/// </summary>
public sealed class TierLedgerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"tierledger-it-{Guid.NewGuid():N}.db");

    public static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
                ["Outbox:PollIntervalSeconds"] = "1",
                ["OverdueScan:Enabled"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
            File.Delete(_databasePath);
    }
}
