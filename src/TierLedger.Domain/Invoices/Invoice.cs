using TierLedger.Domain.Common;
using TierLedger.Domain.Invoices.Events;

namespace TierLedger.Domain.Invoices;

/// <summary>
/// The invoice aggregate. It owns its lines and payments, guards every status
/// transition, and recomputes its totals whenever the lines change.
/// The Paid transition is deliberately not exposed to callers directly:
/// payments are registered here, but only the payment process manager may
/// complete the invoice once it is fully settled (see MarkAsPaid).
/// </summary>
public sealed class Invoice : AggregateRoot
{
    private readonly List<InvoiceLine> _lines = new();
    private readonly List<Payment> _payments = new();

    private Invoice()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Human-readable sequential number, e.g. INV-2026-0001. Assigned by the application layer.</summary>
    public string Number { get; private set; } = string.Empty;

    public Guid ThirdPartyId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    /// <summary>ISO 4217 currency code, e.g. MAD, EUR, USD.</summary>
    public string Currency { get; private set; } = string.Empty;

    public string? Notes { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? PaidOnUtc { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public decimal TotalPaid => _payments.Sum(p => p.Amount);
    public decimal OutstandingAmount => GrossAmount - TotalPaid;

    public static Invoice Create(
        string number,
        Guid thirdPartyId,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        string? notes,
        IEnumerable<InvoiceLine> lines)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Invoice number is required.");
        if (thirdPartyId == Guid.Empty)
            throw new DomainException("Invoice third party is required.");
        if (dueDate < issueDate)
            throw new DomainException("Invoice due date cannot be before the issue date.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Invoice currency must be a 3-letter ISO 4217 code.");

        var lineList = lines?.ToList() ?? throw new DomainException("Invoice must contain at least one line.");
        if (lineList.Count == 0)
            throw new DomainException("Invoice must contain at least one line.");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Number = number.Trim(),
            ThirdPartyId = thirdPartyId,
            Status = InvoiceStatus.Draft,
            IssueDate = issueDate,
            DueDate = dueDate,
            Currency = currency.Trim().ToUpperInvariant(),
            Notes = notes?.Trim(),
            CreatedOnUtc = DateTime.UtcNow,
        };

        invoice._lines.AddRange(lineList);
        invoice.RecalculateTotals();
        invoice.RaiseDomainEvent(new InvoiceCreated(invoice.Id, invoice.ThirdPartyId, invoice.Number, invoice.GrossAmount));
        return invoice;
    }

    /// <summary>Replaces the editable fields and lines. Only possible while the invoice is a draft.</summary>
    public void Revise(DateOnly dueDate, string currency, string? notes, IEnumerable<InvoiceLine> lines)
    {
        EnsureStatus(InvoiceStatus.Draft, "revise");
        if (dueDate < IssueDate)
            throw new DomainException("Invoice due date cannot be before the issue date.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Invoice currency must be a 3-letter ISO 4217 code.");

        var lineList = lines?.ToList() ?? throw new DomainException("Invoice must contain at least one line.");
        if (lineList.Count == 0)
            throw new DomainException("Invoice must contain at least one line.");

        DueDate = dueDate;
        Currency = currency.Trim().ToUpperInvariant();
        Notes = notes?.Trim();
        _lines.Clear();
        _lines.AddRange(lineList);
        RecalculateTotals();
    }

    public void Submit()
    {
        EnsureStatus(InvoiceStatus.Draft, "submit");
        Status = InvoiceStatus.Submitted;
        RaiseDomainEvent(new InvoiceSubmitted(Id, Number));
    }

    public void Approve()
    {
        EnsureStatus(InvoiceStatus.Submitted, "approve");
        Status = InvoiceStatus.Approved;
        RaiseDomainEvent(new InvoiceApproved(Id, ThirdPartyId, Number, GrossAmount, DueDate));
    }

    public void Reject(string reason)
    {
        EnsureStatus(InvoiceStatus.Submitted, "reject");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required to reject an invoice.");

        Status = InvoiceStatus.Rejected;
        RejectionReason = reason.Trim();
        RaiseDomainEvent(new InvoiceRejected(Id, Number, RejectionReason));
    }

    public void ReturnToDraft()
    {
        EnsureStatus(InvoiceStatus.Rejected, "return to draft");
        Status = InvoiceStatus.Draft;
        RejectionReason = null;
        RaiseDomainEvent(new InvoiceReturnedToDraft(Id, Number));
    }

    public void Cancel()
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Submitted))
            throw new DomainException($"Cannot cancel an invoice in status {Status}; only Draft or Submitted invoices can be cancelled.");

        Status = InvoiceStatus.Cancelled;
        RaiseDomainEvent(new InvoiceCancelled(Id, Number));
    }

    /// <summary>
    /// Records a (possibly partial) payment against this approved invoice.
    /// Overpayment is refused. The invoice is NOT marked paid here — the
    /// payment process manager observes the event and completes the invoice
    /// once the outstanding amount reaches zero.
    /// </summary>
    public Payment RegisterPayment(decimal amount, PaymentMethod method, string? reference, DateTime paidOnUtc)
    {
        EnsureStatus(InvoiceStatus.Approved, "register a payment for");
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");
        if (amount > OutstandingAmount)
            throw new DomainException(
                $"Payment of {amount} exceeds the outstanding amount of {OutstandingAmount} on invoice {Number}.");

        var payment = Payment.Create(Id, amount, method, reference, paidOnUtc);
        _payments.Add(payment);
        RaiseDomainEvent(new PaymentRecorded(payment.Id, Id, Number, amount, TotalPaid, GrossAmount));
        return payment;
    }

    /// <summary>Completes the lifecycle. Called by the payment process manager only.</summary>
    public void MarkAsPaid()
    {
        EnsureStatus(InvoiceStatus.Approved, "mark as paid");
        if (OutstandingAmount > 0)
            throw new DomainException($"Invoice {Number} still has an outstanding amount of {OutstandingAmount}.");

        Status = InvoiceStatus.Paid;
        PaidOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new InvoicePaid(Id, ThirdPartyId, Number, GrossAmount));
    }

    private void RecalculateTotals()
    {
        NetAmount = _lines.Sum(l => l.NetAmount);
        TaxAmount = _lines.Sum(l => l.TaxAmount);
        GrossAmount = NetAmount + TaxAmount;
    }

    private void EnsureStatus(InvoiceStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} an invoice in status {Status}; it must be {expected}.");
    }
}
