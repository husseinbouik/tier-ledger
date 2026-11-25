using MediatR;

namespace TierLedger.Application.ThirdParties.GetOutstandingBalance;

public sealed record CurrencyBalanceDto(string Currency, decimal Amount, int OpenInvoiceCount);

/// <summary>
/// Outstanding (approved but not yet fully paid) amounts for one third party,
/// grouped by currency — amounts in different currencies are never summed together.
/// </summary>
public sealed record OutstandingBalanceDto(
    Guid ThirdPartyId,
    IReadOnlyList<CurrencyBalanceDto> Balances,
    int OpenInvoiceCount);

public sealed record GetOutstandingBalanceQuery(Guid ThirdPartyId) : IRequest<OutstandingBalanceDto>;
