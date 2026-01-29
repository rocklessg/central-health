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
}
