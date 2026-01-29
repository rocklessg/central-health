using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Invoices;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetInvoice), new { id = result.Data!.Id, facilityId = request.FacilityId }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid id, [FromQuery] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(id, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices([FromQuery] GetInvoicesRequest request, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoicesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelInvoice(Guid id, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CancelInvoiceAsync(id, request.FacilityId, request.Username, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
