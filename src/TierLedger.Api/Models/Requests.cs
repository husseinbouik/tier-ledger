using TierLedger.Domain.Invoices;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Api.Models;

/// <summary>Response body for creation endpoints.</summary>
public sealed record IdResponse(Guid Id);

public sealed record RegisterThirdPartyRequest(
    string Name,
    ThirdPartyType Type,
    string TaxId,
    string Email,
    string? Phone,
    string Street,
    string City,
    string PostalCode,
    string Country);

public sealed record UpdateThirdPartyRequest(
    string Name,
    string Email,
    string? Phone,
    string Street,
    string City,
    string PostalCode,
    string Country);

public sealed record CreateInvoiceLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent);

public sealed record CreateInvoiceRequest(
    Guid ThirdPartyId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineRequest> Lines);

public sealed record UpdateInvoiceRequest(
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineRequest> Lines);

public sealed record RejectInvoiceRequest(string Reason);

public sealed record RecordPaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTime? PaidOnUtc);
