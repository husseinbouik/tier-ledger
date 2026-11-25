using MediatR;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTime? PaidOnUtc) : IRequest<Guid>;
