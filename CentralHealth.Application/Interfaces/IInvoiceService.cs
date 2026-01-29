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
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        GetInvoicesRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> CancelInvoiceAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default);
}
