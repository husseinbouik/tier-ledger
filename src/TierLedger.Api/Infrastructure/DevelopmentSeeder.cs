using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;
using TierLedger.Infrastructure.Persistence;

namespace TierLedger.Api.Infrastructure;

/// <summary>
/// Development-only seed data so the API can be explored immediately
/// (two third parties and one draft invoice). Runs only when the database
/// is empty, and only in the Development environment.
/// </summary>
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (await context.ThirdParties.AnyAsync())
            return;

        var supplier = ThirdParty.Register(
            "Atlas Office Supplies SARL",
            ThirdPartyType.Supplier,
            "ICE-000111222333444",
            "contact@atlas-office.example",
            "+212 6 00 00 00 00",
            new Address("12 Rue Gauthier", "Casablanca", "20250", "Morocco"));

        var customer = ThirdParty.Register(
            "Nordwind Retail GmbH",
            ThirdPartyType.Customer,
            "DE-136695976",
            "ap@nordwind.example",
            null,
            new Address("44 Hafenstrasse", "Hamburg", "20357", "Germany"));

        context.ThirdParties.AddRange(supplier, customer);

        var numberGenerator = services.GetRequiredService<IInvoiceNumberGenerator>();
        var number = await numberGenerator.NextAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoice = Invoice.Create(
            number,
            supplier.Id,
            today,
            today.AddDays(30),
            "MAD",
            "Seed invoice (development only).",
            new[]
            {
                InvoiceLine.Create("Office chairs", 10, 450m, 20m),
                InvoiceLine.Create("Standing desks", 2, 3200m, 20m),
            });

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
    }
}
