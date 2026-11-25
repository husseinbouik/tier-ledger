using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TierLedger.Application.Processes;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.Invoices.Events;
using TierLedger.Domain.Processes;

namespace TierLedger.UnitTests.Application;

/// <summary>
/// Drives the process manager directly with event payloads — the same objects
/// the outbox dispatcher delivers in production. The integration test suite
/// covers the full HTTP → outbox → process loop.
/// </summary>
public sealed class PaymentProcessManagerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();
    private readonly PaymentProcessManager _manager;

    public PaymentProcessManagerTests()
    {
        _manager = new PaymentProcessManager(_fixture.Context, NullLogger<PaymentProcessManager>.Instance);
    }

    public void Dispose() => _fixture.Dispose();

    private async Task<Invoice> CreateApprovedInvoiceInDbAsync(decimal gross = 240m)
    {
        var thirdParty = global::TierLedger.Domain.ThirdParties.ThirdParty.Register(
            "Atlas Office Supplies SARL",
            global::TierLedger.Domain.ThirdParties.ThirdPartyType.Supplier,
            $"ICE-{Guid.NewGuid():N}"[..20],
            "contact@atlas.example",
            null,
            new global::TierLedger.Domain.ThirdParties.Address("12 Rue Gauthier", "Casablanca", "20250", "Morocco"));
        thirdParty.ClearDomainEvents();
        _fixture.Context.ThirdParties.Add(thirdParty);

        var invoice = Invoice.Create(
            "INV-2026-0001",
            thirdParty.Id,
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 2, 10),
            "MAD",
            null,
            new[] { InvoiceLine.Create("Service", 1, gross, 0m) });
        invoice.Submit();
        invoice.Approve();
        invoice.ClearDomainEvents();

        _fixture.Context.Invoices.Add(invoice);
        await _fixture.Context.SaveChangesAsync();

        // Reload so tests mutate a database-loaded aggregate — the same shape
        // the real command handlers work with.
        _fixture.Context.ChangeTracker.Clear();
        return await _fixture.Context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .SingleAsync(i => i.Id == invoice.Id);
    }

    [Fact]
    public async Task OnInvoiceApproved_StartsProcess_AndDuplicateIsIgnored()
    {
        var invoice = await CreateApprovedInvoiceInDbAsync();
        var approved = new InvoiceApproved(invoice.Id, invoice.ThirdPartyId, invoice.Number, invoice.GrossAmount, invoice.DueDate);

        await _manager.Handle(approved, CancellationToken.None);
        await _manager.Handle(approved, CancellationToken.None);

        var processes = await _fixture.Context.PaymentProcesses.ToListAsync();
        processes.Should().ContainSingle();
        processes[0].Status.Should().Be(PaymentProcessStatus.AwaitingPayment);
        processes[0].TotalDue.Should().Be(240m);
    }

    [Fact]
    public async Task PartialThenFinalPayment_CompletesProcess_AndMarksInvoicePaid()
    {
        var invoice = await CreateApprovedInvoiceInDbAsync();
        await _manager.Handle(
            new InvoiceApproved(invoice.Id, invoice.ThirdPartyId, invoice.Number, invoice.GrossAmount, invoice.DueDate),
            CancellationToken.None);

        // The aggregate records the payments (as RecordPaymentCommandHandler does)…
        invoice.RegisterPayment(100m, PaymentMethod.BankTransfer, "TRX-1", DateTime.UtcNow);
        await _fixture.Context.SaveChangesAsync();

        // …and the process manager reacts to the events.
        await _manager.Handle(
            new PaymentRecorded(Guid.NewGuid(), invoice.Id, invoice.Number, 100m, 100m, 240m),
            CancellationToken.None);

        var process = await _fixture.Context.PaymentProcesses.SingleAsync();
        process.Status.Should().Be(PaymentProcessStatus.AwaitingPayment);
        process.AmountPaid.Should().Be(100m);

        invoice.RegisterPayment(140m, PaymentMethod.BankTransfer, "TRX-2", DateTime.UtcNow);
        await _fixture.Context.SaveChangesAsync();

        await _manager.Handle(
            new PaymentRecorded(Guid.NewGuid(), invoice.Id, invoice.Number, 140m, 240m, 240m),
            CancellationToken.None);

        process.Status.Should().Be(PaymentProcessStatus.Completed);
        process.CompletedOnUtc.Should().NotBeNull();

        var storedInvoice = await _fixture.Context.Invoices.SingleAsync(i => i.Id == invoice.Id);
        storedInvoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task OnInvoiceCancelled_AbortsOpenProcess()
    {
        var invoice = await CreateApprovedInvoiceInDbAsync();
        await _manager.Handle(
            new InvoiceApproved(invoice.Id, invoice.ThirdPartyId, invoice.Number, invoice.GrossAmount, invoice.DueDate),
            CancellationToken.None);

        await _manager.Handle(new InvoiceCancelled(invoice.Id, invoice.Number), CancellationToken.None);

        var process = await _fixture.Context.PaymentProcesses.SingleAsync();
        process.Status.Should().Be(PaymentProcessStatus.Aborted);
    }
}
