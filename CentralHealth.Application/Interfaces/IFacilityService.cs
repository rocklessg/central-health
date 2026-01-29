using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;

namespace CentralHealth.Application.Interfaces;

public interface IFacilityService
{
    Task<ApiResponse<FacilityDto>> CreateFacilityAsync(
        CreateFacilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> GetFacilityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<FacilityDto>>> GetAllFacilitiesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(
        Guid id,
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default);
}
