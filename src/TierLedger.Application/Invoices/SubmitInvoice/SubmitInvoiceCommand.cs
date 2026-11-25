using MediatR;

namespace TierLedger.Application.Invoices.SubmitInvoice;

public sealed record SubmitInvoiceCommand(Guid Id) : IRequest;
