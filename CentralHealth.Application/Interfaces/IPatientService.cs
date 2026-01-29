using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Patients;

namespace CentralHealth.Application.Interfaces;

public interface IPatientService
{
    Task<ApiResponse<PatientDto>> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        GetPatientsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PatientDto>> UpdatePatientAsync(
        Guid id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeletePatientAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> TopUpWalletAsync(
        Guid patientId,
        decimal amount,
        CancellationToken cancellationToken = default);
}
