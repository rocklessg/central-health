using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Users;
using CentralHealth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CentralHealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return result.Success ? CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id, facilityId = request.FacilityId }, result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, [FromQuery] Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, facilityId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateUser(Guid id, [FromBody] CancelRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.DeactivateUserAsync(id, request.FacilityId, request.Username, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
