using MediatR;
using TierLedger.Application.Common.Models;
using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices.ListInvoices;

public sealed record ListInvoicesQuery(
    Guid? ThirdPartyId = null,
    InvoiceStatus? Status = null,
    bool OverdueOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<InvoiceListItemDto>>;
