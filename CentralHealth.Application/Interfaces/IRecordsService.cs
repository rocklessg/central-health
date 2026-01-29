using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Records;

namespace CentralHealth.Application.Interfaces;

public interface IRecordsService
{
    Task<ApiResponse<PagedResult<PatientRecordDto>>> GetRecordsAsync(
        RecordsFilterRequest request,
        CancellationToken cancellationToken = default);
}
