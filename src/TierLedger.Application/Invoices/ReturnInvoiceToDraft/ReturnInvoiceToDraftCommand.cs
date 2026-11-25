using MediatR;

namespace TierLedger.Application.Invoices.ReturnInvoiceToDraft;

public sealed record ReturnInvoiceToDraftCommand(Guid Id) : IRequest;
