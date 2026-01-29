using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;

namespace CentralHealth.Application.Interfaces;

public interface IFacilityService
{
    Task<ApiResponse<CreateFacilityResponse>> CreateFacilityAsync(
        CreateFacilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> GetFacilityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<FacilityDto>>> GetAllFacilitiesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> GetCurrentFacilityAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default);
}
