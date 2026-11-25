using TierLedger.Domain.Common;

namespace TierLedger.Domain.Processes;

public enum PaymentProcessStatus
{
    AwaitingPayment = 0,
    Completed = 1,
    Aborted = 2,
}

/// <summary>
/// State of the payment process manager (a lightweight saga) that follows one
/// invoice from approval to full settlement. It is started by the
/// InvoiceApproved event, accumulates PaymentRecorded events, and completes
/// the invoice when the outstanding amount reaches zero. The overdue scanner
/// flags processes whose due date has passed.
/// </summary>
public sealed class PaymentProcess
{
    private PaymentProcess()
    {
    }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid ThirdPartyId { get; private set; }
    public PaymentProcessStatus Status { get; private set; }
    public decimal TotalDue { get; private set; }
    public decimal AmountPaid { get; private set; }
    public DateOnly DueDate { get; private set; }
    public bool IsOverdue { get; private set; }
    public DateTime? OverdueSinceUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }

    public decimal OutstandingAmount => TotalDue - AmountPaid;
    public bool IsFullyPaid => OutstandingAmount <= 0;

    public static PaymentProcess Start(Guid invoiceId, Guid thirdPartyId, decimal totalDue, DateOnly dueDate)
    {
        if (totalDue <= 0)
            throw new DomainException("A payment process requires a positive amount due.");

        return new PaymentProcess
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ThirdPartyId = thirdPartyId,
            Status = PaymentProcessStatus.AwaitingPayment,
            TotalDue = totalDue,
            AmountPaid = 0m,
            DueDate = dueDate,
            CreatedOnUtc = DateTime.UtcNow,
        };
    }

    /// <summary>Applies a payment and returns true when the process is fully settled.</summary>
    public bool ApplyPayment(decimal amount)
    {
        if (Status != PaymentProcessStatus.AwaitingPayment)
            throw new DomainException($"Cannot apply a payment to a payment process in status {Status}.");
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        AmountPaid += amount;
        return IsFullyPaid;
    }

    public void Complete()
    {
        if (Status != PaymentProcessStatus.AwaitingPayment)
            throw new DomainException($"Cannot complete a payment process in status {Status}.");
        if (!IsFullyPaid)
            throw new DomainException("Cannot complete a payment process that is not fully paid.");

        Status = PaymentProcessStatus.Completed;
        CompletedOnUtc = DateTime.UtcNow;
    }

    public void Abort()
    {
        if (Status == PaymentProcessStatus.Completed)
            throw new DomainException("Cannot abort a completed payment process.");

        Status = PaymentProcessStatus.Aborted;
    }

    public void MarkOverdue(DateTime nowUtc)
    {
        if (Status != PaymentProcessStatus.AwaitingPayment || IsOverdue)
            return;

        IsOverdue = true;
        OverdueSinceUtc = nowUtc;
    }
}
