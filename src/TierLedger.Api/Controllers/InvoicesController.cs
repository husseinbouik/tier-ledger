using MediatR;
using Microsoft.AspNetCore.Mvc;
using TierLedger.Api.Models;
using TierLedger.Application.Common.Models;
using TierLedger.Application.Invoices;
using TierLedger.Application.Invoices.ApproveInvoice;
using TierLedger.Application.Invoices.CancelInvoice;
using TierLedger.Application.Invoices.CreateInvoice;
using TierLedger.Application.Invoices.GetInvoiceById;
using TierLedger.Application.Invoices.GetPaymentProcess;
using TierLedger.Application.Invoices.ListInvoices;
using TierLedger.Application.Invoices.RecordPayment;
using TierLedger.Application.Invoices.RejectInvoice;
using TierLedger.Application.Invoices.ReturnInvoiceToDraft;
using TierLedger.Application.Invoices.SubmitInvoice;
using TierLedger.Application.Invoices.UpdateInvoice;
using TierLedger.Domain.Invoices;

namespace TierLedger.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Produces("application/json")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Creates a draft invoice. The number (INV-yyyy-NNNN) is assigned by the server.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdResponse>> Create(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateInvoiceCommand(
            request.ThirdPartyId,
            request.IssueDate,
            request.DueDate,
            request.Currency,
            request.Notes,
            request.Lines.Select(ToLineDto).ToList()), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new IdResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetInvoiceByIdQuery(id), cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvoiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> List(
        [FromQuery] Guid? thirdPartyId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] bool overdueOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new ListInvoicesQuery(thirdPartyId, status, overdueOnly, page, pageSize),
            cancellationToken));

    /// <summary>Revises a draft invoice (due date, currency, notes, full line set).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateInvoiceCommand(
            id,
            request.DueDate,
            request.Currency,
            request.Notes,
            request.Lines.Select(ToLineDto).ToList()), cancellationToken);

        return NoContent();
    }

    /// <summary>Draft → Submitted.</summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SubmitInvoiceCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Submitted → Approved. Starts the payment process for this invoice.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ApproveInvoiceCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Submitted → Rejected (a reason is required).</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid id,
        RejectInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RejectInvoiceCommand(id, request.Reason), cancellationToken);
        return NoContent();
    }

    /// <summary>Rejected → Draft, so the invoice can be revised and resubmitted.</summary>
    [HttpPost("{id:guid}/return-to-draft")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnToDraft(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReturnInvoiceToDraftCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Draft/Submitted → Cancelled. Aborts the payment process if one exists.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelInvoiceCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Records a (possibly partial) payment against an approved invoice.
    /// When payments cover the full amount, the payment process manager marks
    /// the invoice Paid — asynchronously, via the outbox.
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdResponse>> RecordPayment(
        Guid id,
        RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var paymentId = await _mediator.Send(new RecordPaymentCommand(
            id,
            request.Amount,
            request.Method,
            request.Reference,
            request.PaidOnUtc), cancellationToken);

        return Created($"/api/invoices/{id}", new IdResponse(paymentId));
    }

    /// <summary>The payment process (saga) state for this invoice: amounts, due date, overdue flag.</summary>
    [HttpGet("{id:guid}/payment-process")]
    [ProducesResponseType(typeof(PaymentProcessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentProcessDto>> GetPaymentProcess(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetPaymentProcessQuery(id), cancellationToken));

    private static CreateInvoiceLineDto ToLineDto(CreateInvoiceLineRequest line) => new(
        line.Description,
        line.Quantity,
        line.UnitPrice,
        line.TaxRatePercent);
}
