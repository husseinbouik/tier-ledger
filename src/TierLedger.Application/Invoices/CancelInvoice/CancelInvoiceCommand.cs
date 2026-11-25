using MediatR;

namespace TierLedger.Application.Invoices.CancelInvoice;

public sealed record CancelInvoiceCommand(Guid Id) : IRequest;
