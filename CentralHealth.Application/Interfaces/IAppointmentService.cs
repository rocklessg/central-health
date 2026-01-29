using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Appointments;

namespace CentralHealth.Application.Interfaces;

public interface IAppointmentService
{
    Task<ApiResponse<AppointmentDto>> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AppointmentDto>>> GetAppointmentsAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? clinicId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> CancelAppointmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
