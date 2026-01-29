using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.MedicalServices;

namespace CentralHealth.Application.Interfaces;

public interface IMedicalServiceService
{
    Task<ApiResponse<MedicalServiceDto>> CreateMedicalServiceAsync(
        CreateMedicalServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MedicalServiceDto>> GetMedicalServiceByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<MedicalServiceDto>>> GetMedicalServicesAsync(
        GetMedicalServicesRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<MedicalServiceDto>> UpdateMedicalServiceAsync(
        Guid id,
        UpdateMedicalServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteMedicalServiceAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default);
}
