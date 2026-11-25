using MediatR;

namespace TierLedger.Application.Invoices.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;
