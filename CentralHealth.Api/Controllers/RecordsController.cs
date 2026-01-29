using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Records;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordsController : ControllerBase
{
    private readonly IRecordsService _recordsService;

    public RecordsController(IRecordsService recordsService)
    {
        _recordsService = recordsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientRecordDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecords([FromQuery] RecordsFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _recordsService.GetRecordsAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
