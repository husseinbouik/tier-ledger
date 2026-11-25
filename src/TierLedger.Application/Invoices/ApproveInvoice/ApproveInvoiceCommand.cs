using MediatR;

namespace TierLedger.Application.Invoices.ApproveInvoice;

public sealed record ApproveInvoiceCommand(Guid Id) : IRequest;
