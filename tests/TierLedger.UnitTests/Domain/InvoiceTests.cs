using FluentAssertions;
using TierLedger.Domain.Common;
using TierLedger.Domain.Invoices;
using TierLedger.Domain.Invoices.Events;

namespace TierLedger.UnitTests.Domain;

public sealed class InvoiceTests
{
    private static readonly Guid ThirdPartyId = Guid.NewGuid();
    private static readonly DateOnly IssueDate = new(2026, 1, 10);
    private static readonly DateOnly DueDate = new(2026, 2, 10);

    private static Invoice CreateDraft(params InvoiceLine[] lines) =>
        Invoice.Create("INV-2026-0001", ThirdPartyId, IssueDate, DueDate, "MAD", null, lines);

    private static Invoice CreateApproved(decimal amount)
    {
        var invoice = CreateDraft(InvoiceLine.Create("Service", 1, amount, 0m));
        invoice.Submit();
        invoice.Approve();
        return invoice;
    }

    [Fact]
    public void Create_ComputesTotals_WithMixedTaxRates()
    {
        var invoice = CreateDraft(
            InvoiceLine.Create("Consulting", 2, 100m, 20m),
            InvoiceLine.Create("Hosting", 1, 50m, 10m));

        invoice.NetAmount.Should().Be(250m);
        invoice.TaxAmount.Should().Be(45m);
        invoice.GrossAmount.Should().Be(295m);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceCreated);
    }

    [Fact]
    public void LineTax_RoundsHalfAwayFromZero()
    {
        // 3 x 33.33 = 99.99 net; 20% of 99.99 = 19.998 -> 20.00 tax.
        var line = InvoiceLine.Create("Rounded", 3, 33.33m, 20m);

        line.NetAmount.Should().Be(99.99m);
        line.TaxAmount.Should().Be(20.00m);
        line.GrossAmount.Should().Be(119.99m);
    }

    [Fact]
    public void Create_WithoutLines_Throws()
    {
        var act = () => CreateDraft();

        act.Should().Throw<DomainException>().WithMessage("*at least one line*");
    }

    [Fact]
    public void Create_DueDateBeforeIssueDate_Throws()
    {
        var act = () => Invoice.Create(
            "INV-2026-0001",
            ThirdPartyId,
            IssueDate,
            IssueDate.AddDays(-1),
            "MAD",
            null,
            new[] { InvoiceLine.Create("X", 1, 10m, 20m) });

        act.Should().Throw<DomainException>().WithMessage("*due date*");
    }

    [Fact]
    public void Submit_FromDraft_SetsSubmittedAndRaisesEvent()
    {
        var invoice = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));

        invoice.Submit();

        invoice.Status.Should().Be(InvoiceStatus.Submitted);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceSubmitted);
    }

    [Fact]
    public void Approve_FromDraft_Throws()
    {
        var invoice = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));

        var act = () => invoice.Approve();

        act.Should().Throw<DomainException>().WithMessage("*must be Submitted*");
    }

    [Fact]
    public void Reject_FromSubmitted_SetsReason_AndRejectWithoutReason_Throws()
    {
        var invoice = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));
        invoice.Submit();

        var noReason = () => invoice.Reject(" ");
        noReason.Should().Throw<DomainException>().WithMessage("*reason*");

        invoice.Reject("Wrong tax rate");
        invoice.Status.Should().Be(InvoiceStatus.Rejected);
        invoice.RejectionReason.Should().Be("Wrong tax rate");
    }

    [Fact]
    public void ReturnToDraft_FromRejected_ClearsReason()
    {
        var invoice = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));
        invoice.Submit();
        invoice.Reject("Fix the address");

        invoice.ReturnToDraft();

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Cancel_FromApproved_Throws_ButFromDraft_Works()
    {
        var approved = CreateApproved(100m);
        var act = () => approved.Cancel();
        act.Should().Throw<DomainException>();

        var draft = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));
        draft.Cancel();
        draft.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void RegisterPayment_OnDraftInvoice_Throws()
    {
        var invoice = CreateDraft(InvoiceLine.Create("X", 1, 10m, 20m));

        var act = () => invoice.RegisterPayment(10m, PaymentMethod.Card, null, DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*must be Approved*");
    }

    [Fact]
    public void RegisterPayment_ExceedingOutstanding_Throws()
    {
        var invoice = CreateApproved(100m);
        invoice.RegisterPayment(60m, PaymentMethod.BankTransfer, "TRX-1", DateTime.UtcNow);

        var act = () => invoice.RegisterPayment(40.01m, PaymentMethod.BankTransfer, "TRX-2", DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*exceeds the outstanding amount*");
    }

    [Fact]
    public void RegisterPayment_Partial_KeepsInvoiceApproved_WithOutstanding()
    {
        var invoice = CreateApproved(100m);

        var payment = invoice.RegisterPayment(30m, PaymentMethod.Cash, null, DateTime.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Approved);
        invoice.TotalPaid.Should().Be(30m);
        invoice.OutstandingAmount.Should().Be(70m);
        invoice.Payments.Should().ContainSingle().Which.Should().BeSameAs(payment);
        invoice.DomainEvents.Should().ContainSingle(e => e is PaymentRecorded);
    }

    [Fact]
    public void MarkAsPaid_WithOutstanding_Throws_AndAfterFullPayment_Succeeds()
    {
        var invoice = CreateApproved(100m);
        invoice.RegisterPayment(40m, PaymentMethod.Card, null, DateTime.UtcNow);

        var act = () => invoice.MarkAsPaid();
        act.Should().Throw<DomainException>().WithMessage("*outstanding amount*");

        invoice.RegisterPayment(60m, PaymentMethod.Card, null, DateTime.UtcNow);
        invoice.MarkAsPaid();

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidOnUtc.Should().NotBeNull();
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoicePaid);
    }
}
