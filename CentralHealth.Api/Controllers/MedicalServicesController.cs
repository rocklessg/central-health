using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.MedicalServices;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalServicesController : ControllerBase
{
    private readonly IMedicalServiceService _medicalServiceService;

    public MedicalServicesController(IMedicalServiceService medicalServiceService)
    {
        _medicalServiceService = medicalServiceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MedicalServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMedicalService([FromBody] CreateMedicalServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _medicalServiceService.CreateMedicalServiceAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetMedicalService), new { id = result.Data!.Id, facilityId = request.FacilityId }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedicalService(Guid id, [FromQuery] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _medicalServiceService.GetMedicalServiceByIdAsync(id, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalServices([FromQuery] GetMedicalServicesRequest request, CancellationToken cancellationToken)
    {
        var result = await _medicalServiceService.GetMedicalServicesAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicalServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMedicalService(Guid id, [FromBody] UpdateMedicalServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await _medicalServiceService.UpdateMedicalServiceAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMedicalService(Guid id, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        var result = await _medicalServiceService.DeleteMedicalServiceAsync(id, request.FacilityId, request.Username, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
