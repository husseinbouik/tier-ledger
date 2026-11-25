using MediatR;

namespace TierLedger.Application.Invoices.CreateInvoice;

public sealed record CreateInvoiceLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent);

public sealed record CreateInvoiceCommand(
    Guid ThirdPartyId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineDto> Lines) : IRequest<Guid>;
