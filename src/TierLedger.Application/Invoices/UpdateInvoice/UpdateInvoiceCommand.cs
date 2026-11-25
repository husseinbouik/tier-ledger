using MediatR;
using TierLedger.Application.Invoices.CreateInvoice;

namespace TierLedger.Application.Invoices.UpdateInvoice;

/// <summary>Revises a draft invoice: due date, currency, notes and the full line set.</summary>
public sealed record UpdateInvoiceCommand(
    Guid Id,
    DateOnly DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineDto> Lines) : IRequest;
