using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Patients;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await _patientService.CreatePatientAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetPatient), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetPatientByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatients([FromQuery] GetPatientsRequest request, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetPatientsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await _patientService.UpdatePatientAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePatient(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.DeletePatientAsync(id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/wallet/topup")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TopUpWallet(Guid id, [FromBody] decimal amount, CancellationToken cancellationToken)
    {
        var result = await _patientService.TopUpWalletAsync(id, amount, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
