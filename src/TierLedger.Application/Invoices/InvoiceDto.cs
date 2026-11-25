using TierLedger.Domain.Invoices;

namespace TierLedger.Application.Invoices;

public sealed record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount);

public sealed record PaymentDto(
    Guid Id,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    DateTime PaidOnUtc);

public sealed record InvoiceDto(
    Guid Id,
    string Number,
    Guid ThirdPartyId,
    InvoiceStatus Status,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    decimal NetAmount,
    decimal TaxAmount,
    decimal GrossAmount,
    decimal TotalPaid,
    decimal OutstandingAmount,
    bool IsOverdue,
    string? RejectionReason,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments,
    DateTime CreatedOnUtc,
    DateTime? PaidOnUtc);

public sealed record InvoiceListItemDto(
    Guid Id,
    string Number,
    Guid ThirdPartyId,
    InvoiceStatus Status,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Currency,
    decimal GrossAmount,
    decimal OutstandingAmount,
    bool IsOverdue);

public static class InvoiceMappings
{
    public static bool IsOverdue(this Invoice invoice) =>
        invoice.Status == InvoiceStatus.Approved
        && invoice.DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.Number,
        invoice.ThirdPartyId,
        invoice.Status,
        invoice.IssueDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.Notes,
        invoice.NetAmount,
        invoice.TaxAmount,
        invoice.GrossAmount,
        invoice.TotalPaid,
        invoice.OutstandingAmount,
        invoice.IsOverdue(),
        invoice.RejectionReason,
        invoice.Lines.Select(l => new InvoiceLineDto(
            l.Id,
            l.Description,
            l.Quantity,
            l.UnitPrice,
            l.TaxRatePercent,
            l.NetAmount,
            l.TaxAmount,
            l.GrossAmount)).ToList(),
        invoice.Payments.Select(p => new PaymentDto(
            p.Id,
            p.Amount,
            p.Method,
            p.Reference,
            p.PaidOnUtc)).ToList(),
        invoice.CreatedOnUtc,
        invoice.PaidOnUtc);

    public static InvoiceListItemDto ToListItemDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.Number,
        invoice.ThirdPartyId,
        invoice.Status,
        invoice.IssueDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.GrossAmount,
        invoice.OutstandingAmount,
        invoice.IsOverdue());
}
