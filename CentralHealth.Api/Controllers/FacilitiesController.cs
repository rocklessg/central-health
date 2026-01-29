using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController : ControllerBase
{
    private readonly IFacilityService _facilityService;

    public FacilitiesController(IFacilityService facilityService)
    {
        _facilityService = facilityService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateFacilityResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateFacilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.CreateFacilityAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetFacility), new { id = result.Data!.Facility.Id }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFacility(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetFacilityByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
