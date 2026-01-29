using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Invoices;

namespace CentralHealth.Application.Interfaces;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        Guid? patientId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> CancelInvoiceAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
