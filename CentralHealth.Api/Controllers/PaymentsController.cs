using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Payments;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessPaymentAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetPayment), new { id = result.Data!.Id, facilityId = request.FacilityId }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id, [FromQuery] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentByIdAsync(id, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("invoice/{invoiceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentsByInvoice(Guid invoiceId, [FromQuery] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentsByInvoiceIdAsync(invoiceId, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
