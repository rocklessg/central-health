using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Appointments;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreateAppointmentAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetAppointment), new { id = result.Data!.Id, facilityId = request.FacilityId }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointment(Guid id, [FromQuery] [Required] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments([FromQuery] GetAppointmentsRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetAppointmentsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CancelAppointmentAsync(id, request.FacilityId, request.Username, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
