using MediatR;
using TierLedger.Application.Common.Models;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Application.ThirdParties.ListThirdParties;

public sealed record ListThirdPartiesQuery(
    ThirdPartyType? Type = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ThirdPartyListItemDto>>;
