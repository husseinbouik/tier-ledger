using TierLedger.Domain.Common;

namespace TierLedger.Domain.Invoices.Events;

public sealed record InvoiceCreated(Guid InvoiceId, Guid ThirdPartyId, string Number, decimal GrossAmount) : IDomainEvent;

public sealed record InvoiceSubmitted(Guid InvoiceId, string Number) : IDomainEvent;

public sealed record InvoiceApproved(Guid InvoiceId, Guid ThirdPartyId, string Number, decimal GrossAmount, DateOnly DueDate) : IDomainEvent;

public sealed record InvoiceRejected(Guid InvoiceId, string Number, string Reason) : IDomainEvent;

public sealed record InvoiceReturnedToDraft(Guid InvoiceId, string Number) : IDomainEvent;

public sealed record InvoiceCancelled(Guid InvoiceId, string Number) : IDomainEvent;

public sealed record PaymentRecorded(Guid PaymentId, Guid InvoiceId, string Number, decimal Amount, decimal TotalPaid, decimal GrossAmount) : IDomainEvent;

public sealed record InvoicePaid(Guid InvoiceId, Guid ThirdPartyId, string Number, decimal GrossAmount) : IDomainEvent;

/// <summary>Raised by the overdue scanner (via the payment process) when an approved invoice passes its due date unpaid.</summary>
public sealed record InvoiceOverdue(Guid InvoiceId, Guid ThirdPartyId, string Number, DateOnly DueDate, decimal OutstandingAmount) : IDomainEvent;
