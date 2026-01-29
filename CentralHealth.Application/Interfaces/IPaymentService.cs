using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Payments;

namespace CentralHealth.Application.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsByInvoiceIdAsync(
        Guid invoiceId,
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
