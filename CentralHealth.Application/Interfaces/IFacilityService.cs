using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;

namespace CentralHealth.Application.Interfaces;

public interface IFacilityService
{
    Task<ApiResponse<FacilityDto>> GetCurrentFacilityAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default);
}
