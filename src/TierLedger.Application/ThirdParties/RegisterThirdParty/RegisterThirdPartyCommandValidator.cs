using FluentValidation;

namespace TierLedger.Application.ThirdParties.RegisterThirdParty;

public sealed class RegisterThirdPartyCommandValidator : AbstractValidator<RegisterThirdPartyCommand>
{
    public RegisterThirdPartyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(16);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}
