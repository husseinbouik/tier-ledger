using MediatR;

namespace TierLedger.Application.Invoices.RejectInvoice;

public sealed record RejectInvoiceCommand(Guid Id, string Reason) : IRequest;
