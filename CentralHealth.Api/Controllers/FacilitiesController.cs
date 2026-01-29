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
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateFacilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.CreateFacilityAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetFacility), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFacility(Guid id, CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetFacilityByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FacilityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFacilities(CancellationToken cancellationToken)
    {
        var result = await _facilityService.GetAllFacilitiesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFacility(Guid id, [FromBody] UpdateFacilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _facilityService.UpdateFacilityAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
