using MediatR;
using Microsoft.EntityFrameworkCore;
using TierLedger.Application.Common.Exceptions;
using TierLedger.Application.Common.Interfaces;
using TierLedger.Domain.Processes;

namespace TierLedger.Application.Invoices.GetPaymentProcess;

public sealed class GetPaymentProcessQueryHandler : IRequestHandler<GetPaymentProcessQuery, PaymentProcessDto>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentProcessQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaymentProcessDto> Handle(GetPaymentProcessQuery request, CancellationToken cancellationToken)
    {
        var process = await _context.PaymentProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InvoiceId == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentProcess), request.InvoiceId);

        return new PaymentProcessDto(
            process.Id,
            process.InvoiceId,
            process.Status,
            process.TotalDue,
            process.AmountPaid,
            process.OutstandingAmount,
            process.DueDate,
            process.IsOverdue,
            process.OverdueSinceUtc,
            process.CreatedOnUtc,
            process.CompletedOnUtc);
    }
}
