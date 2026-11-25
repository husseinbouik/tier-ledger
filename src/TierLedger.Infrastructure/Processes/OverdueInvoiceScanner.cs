using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TierLedger.Domain.Invoices.Events;
using TierLedger.Domain.Processes;
using TierLedger.Infrastructure.Outbox;
using TierLedger.Infrastructure.Persistence;

namespace TierLedger.Infrastructure.Processes;

/// <summary>
/// The time-based side of the payment process manager: periodically flags
/// payment processes whose due date has passed and raises an InvoiceOverdue
/// event for each of them — through the outbox, like every other event in the
/// system, so it is dispatched and audited the same way.
/// </summary>
public sealed class OverdueInvoiceScanner : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueInvoiceScanner> _logger;
    private readonly OverdueScanOptions _options;

    public OverdueInvoiceScanner(
        IServiceScopeFactory scopeFactory,
        IOptions<OverdueScanOptions> options,
        ILogger<OverdueInvoiceScanner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Overdue invoice scanner is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes)));

        do
        {
            try
            {
                await ScanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue invoice scan failed; will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueProcesses = await context.PaymentProcesses
            .Where(p => p.Status == PaymentProcessStatus.AwaitingPayment && !p.IsOverdue && p.DueDate < today)
            .ToListAsync(cancellationToken);

        if (overdueProcesses.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var process in overdueProcesses)
        {
            var invoiceNumber = await context.Invoices
                .Where(i => i.Id == process.InvoiceId)
                .Select(i => i.Number)
                .FirstAsync(cancellationToken);

            process.MarkOverdue(now);

            var domainEvent = new InvoiceOverdue(
                process.InvoiceId,
                process.ThirdPartyId,
                invoiceNumber,
                process.DueDate,
                process.OutstandingAmount);

            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                OccurredOnUtc = now,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Flagged {Count} invoice(s) as overdue.", overdueProcesses.Count);
    }
}
