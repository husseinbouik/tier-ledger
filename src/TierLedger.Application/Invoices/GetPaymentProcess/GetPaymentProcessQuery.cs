using MediatR;
using TierLedger.Domain.Processes;

namespace TierLedger.Application.Invoices.GetPaymentProcess;

public sealed record PaymentProcessDto(
    Guid Id,
    Guid InvoiceId,
    PaymentProcessStatus Status,
    decimal TotalDue,
    decimal AmountPaid,
    decimal OutstandingAmount,
    DateOnly DueDate,
    bool IsOverdue,
    DateTime? OverdueSinceUtc,
    DateTime CreatedOnUtc,
    DateTime? CompletedOnUtc);

public sealed record GetPaymentProcessQuery(Guid InvoiceId) : IRequest<PaymentProcessDto>;
