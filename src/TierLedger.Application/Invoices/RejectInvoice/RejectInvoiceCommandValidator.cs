using FluentValidation;

namespace TierLedger.Application.Invoices.RejectInvoice;

public sealed class RejectInvoiceCommandValidator : AbstractValidator<RejectInvoiceCommand>
{
    public RejectInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
