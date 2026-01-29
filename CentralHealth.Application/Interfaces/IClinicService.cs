using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Clinics;

namespace CentralHealth.Application.Interfaces;

public interface IClinicService
{
    Task<ApiResponse<ClinicDto>> CreateClinicAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ClinicDto>> GetClinicByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<ClinicDto>>> GetClinicsAsync(
        GetClinicsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ClinicDto>> UpdateClinicAsync(
        Guid id,
        UpdateClinicRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteClinicAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default);
}
