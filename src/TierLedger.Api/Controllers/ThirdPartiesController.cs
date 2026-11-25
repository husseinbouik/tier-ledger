using MediatR;
using Microsoft.AspNetCore.Mvc;
using TierLedger.Api.Models;
using TierLedger.Application.Common.Models;
using TierLedger.Application.ThirdParties;
using TierLedger.Application.ThirdParties.DeactivateThirdParty;
using TierLedger.Application.ThirdParties.GetOutstandingBalance;
using TierLedger.Application.ThirdParties.GetThirdPartyById;
using TierLedger.Application.ThirdParties.ListThirdParties;
using TierLedger.Application.ThirdParties.RegisterThirdParty;
using TierLedger.Application.ThirdParties.UpdateThirdParty;
using TierLedger.Domain.ThirdParties;

namespace TierLedger.Api.Controllers;

[ApiController]
[Route("api/third-parties")]
[Produces("application/json")]
public sealed class ThirdPartiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ThirdPartiesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Registers a new third party (customer, supplier, or both).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdResponse>> Register(
        RegisterThirdPartyRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new RegisterThirdPartyCommand(
            request.Name,
            request.Type,
            request.TaxId,
            request.Email,
            request.Phone,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ThirdPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThirdPartyDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetThirdPartyByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ThirdPartyListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ThirdPartyListItemDto>>> List(
        [FromQuery] ThirdPartyType? type,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new ListThirdPartiesQuery(type, isActive, search, page, pageSize),
            cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateThirdPartyRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateThirdPartyCommand(
            id,
            request.Name,
            request.Email,
            request.Phone,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country), cancellationToken);

        return NoContent();
    }

    /// <summary>Deactivates a third party. Deactivated parties cannot receive new invoices.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateThirdPartyCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Approved-but-unpaid amounts for this third party, grouped by currency.</summary>
    [HttpGet("{id:guid}/outstanding-balance")]
    [ProducesResponseType(typeof(OutstandingBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OutstandingBalanceDto>> GetOutstandingBalance(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetOutstandingBalanceQuery(id), cancellationToken));
}
