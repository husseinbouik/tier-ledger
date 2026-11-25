using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Invoices.Events;
using TierLedger.Domain.Processes;

namespace TierLedger.Application.Processes;

/// <summary>
/// The payment process manager — a lightweight saga that coordinates the
/// "approved invoice gets settled" flow:
///
///   InvoiceApproved  → start a PaymentProcess (amount due, due date)
///   PaymentRecorded  → accumulate; when fully paid, complete the process
///                      and mark the invoice Paid
///   InvoiceCancelled → abort the process
///
/// Events reach this class through the transactional outbox, so delivery is
/// at-least-once: every handler is idempotent. Note the deliberate split of
/// responsibilities — the invoice aggregate records payments and guards the
/// money, but only this process manager may complete the lifecycle.
/// </summary>
public sealed class PaymentProcessManager :
    INotificationHandler<InvoiceApproved>,
    INotificationHandler<PaymentRecorded>,
    INotificationHandler<InvoiceCancelled>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentProcessManager> _logger;

    public PaymentProcessManager(IApplicationDbContext context, ILogger<PaymentProcessManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(InvoiceApproved notification, CancellationToken cancellationToken)
    {
        var alreadyStarted = await _context.PaymentProcesses
            .AnyAsync(p => p.InvoiceId == notification.InvoiceId, cancellationToken);

        if (alreadyStarted)
        {
            _logger.LogDebug(
                "Payment process for invoice {InvoiceNumber} already exists; ignoring duplicate InvoiceApproved.",
                notification.Number);
            return;
        }

        var process = PaymentProcess.Start(
            notification.InvoiceId,
            notification.ThirdPartyId,
            notification.GrossAmount,
            notification.DueDate);

        _context.PaymentProcesses.Add(process);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment process started for invoice {InvoiceNumber}: {Amount} due by {DueDate}.",
            notification.Number,
            notification.GrossAmount,
            notification.DueDate);
    }

    public async Task Handle(PaymentRecorded notification, CancellationToken cancellationToken)
    {
        var process = await _context.PaymentProcesses
            .FirstOrDefaultAsync(p => p.InvoiceId == notification.InvoiceId, cancellationToken);

        if (process is null)
        {
            _logger.LogWarning(
                "Payment recorded for invoice {InvoiceNumber} but no payment process exists; ignoring.",
                notification.Number);
            return;
        }

        if (process.Status != PaymentProcessStatus.AwaitingPayment)
        {
            _logger.LogDebug(
                "Payment process for invoice {InvoiceNumber} is {Status}; ignoring duplicate PaymentRecorded.",
                notification.Number,
                process.Status);
            return;
        }

        var fullyPaid = process.ApplyPayment(notification.Amount);

        if (fullyPaid)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == notification.InvoiceId, cancellationToken);

            invoice.MarkAsPaid();
            process.Complete();

            _logger.LogInformation(
                "Invoice {InvoiceNumber} fully settled and marked as Paid.",
                notification.Number);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(InvoiceCancelled notification, CancellationToken cancellationToken)
    {
        var process = await _context.PaymentProcesses
            .FirstOrDefaultAsync(p => p.InvoiceId == notification.InvoiceId, cancellationToken);

        if (process is null || process.Status != PaymentProcessStatus.AwaitingPayment)
            return;

        process.Abort();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment process for cancelled invoice {InvoiceNumber} aborted.",
            notification.Number);
    }
}
