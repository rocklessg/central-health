using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Clinics;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClinicDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicRequest request, CancellationToken cancellationToken)
    {
        var result = await _clinicService.CreateClinicAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetClinic), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClinic(Guid id, CancellationToken cancellationToken)
    {
        var result = await _clinicService.GetClinicByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ClinicDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClinics([FromQuery] GetClinicsRequest request, CancellationToken cancellationToken)
    {
        var result = await _clinicService.GetClinicsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ClinicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateClinic(Guid id, [FromBody] UpdateClinicRequest request, CancellationToken cancellationToken)
    {
        var result = await _clinicService.UpdateClinicAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteClinic(Guid id, CancellationToken cancellationToken)
    {
        var result = await _clinicService.DeleteClinicAsync(id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
