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

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentFacility(CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetCurrentFacilityAsync(cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFacility([FromBody] UpdateFacilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.UpdateFacilityAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
