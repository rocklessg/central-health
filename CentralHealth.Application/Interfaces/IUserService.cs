using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Users;

namespace CentralHealth.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDto>> GetUserByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDto>> UpdateUserAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeactivateUserAsync(
        Guid id,
        Guid facilityId,
        Guid currentUserId,
        string username,
        CancellationToken cancellationToken = default);
}
