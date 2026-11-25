using FluentValidation;

namespace TierLedger.Application.ThirdParties.ListThirdParties;

public sealed class ListThirdPartiesQueryValidator : AbstractValidator<ListThirdPartiesQuery>
{
    public ListThirdPartiesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
