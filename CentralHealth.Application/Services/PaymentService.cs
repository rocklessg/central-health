using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Invoices;
using CentralHealth.Application.DTOs.Payments;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<Invoice> _invoiceRepository;
    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly IRepository<PatientWallet> _walletRepository;
    private readonly IRepository<WalletTransaction> _walletTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IRepository<Payment> paymentRepository,
        IRepository<Invoice> invoiceRepository,
        IRepository<Appointment> appointmentRepository,
        IRepository<PatientWallet> walletRepository,
        IRepository<WalletTransaction> walletTransactionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _appointmentRepository = appointmentRepository;
        _walletRepository = walletRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var facilityId = _currentUserService.FacilityId;

            var invoice = await _invoiceRepository.Query()
                .Include(i => i.Appointment)
                .Include(i => i.Patient)
                    .ThenInclude(p => p.Wallet)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.FacilityId == facilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found. InvoiceId={InvoiceId}", request.InvoiceId);
                return ApiResponse<PaymentDto>.FailureResponse("Invoice not found");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                return ApiResponse<PaymentDto>.FailureResponse("Invoice is already paid");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return ApiResponse<PaymentDto>.FailureResponse("Cannot process payment for a cancelled invoice");
            }

            if (request.Amount > invoice.OutstandingAmount)
            {
                return ApiResponse<PaymentDto>.FailureResponse(
                    $"Payment amount ({request.Amount:N2}) exceeds outstanding amount ({invoice.OutstandingAmount:N2})");
            }

            if (request.Method == PaymentMethod.Wallet)
            {
                var wallet = invoice.Patient.Wallet;
                if (wallet == null || wallet.Balance < request.Amount)
                {
                    return ApiResponse<PaymentDto>.FailureResponse("Insufficient wallet balance");
                }

                var balanceBefore = wallet.Balance;
                wallet.Balance -= request.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepository.Update(wallet);

                var walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Amount = -request.Amount,
                    TransactionType = "PAYMENT",
                    Description = $"Payment for Invoice {invoice.InvoiceNumber}",
                    Reference = request.TransactionId,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.Username
                };

                await _walletTransactionRepository.AddAsync(walletTransaction, cancellationToken);
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PaymentReference = GeneratePaymentReference(),
                InvoiceId = request.InvoiceId,
                Amount = request.Amount,
                Currency = invoice.Currency,
                Method = request.Method,
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow,
                TransactionId = request.TransactionId,
                Notes = request.Notes,
                ProcessedByUserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            invoice.PaidAmount += request.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = _currentUserService.Username;

            _invoiceRepository.Update(invoice);

            if (invoice.Status == InvoiceStatus.Paid && invoice.Appointment != null)
            {
                var appointment = invoice.Appointment;
                if (appointment.Status == AppointmentStatus.AwaitingPayment)
                {
                    appointment.Status = AppointmentStatus.AwaitingVitals;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    appointment.UpdatedBy = _currentUserService.Username;
                    _appointmentRepository.Update(appointment);

                    _logger.LogInformation(
                        "Appointment status updated to AwaitingVitals. AppointmentId={AppointmentId}",
                        appointment.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Payment processed successfully. PaymentId={PaymentId}, InvoiceId={InvoiceId}, Amount={Amount}, Method={Method}",
                payment.Id, invoice.Id, request.Amount, request.Method);

            var dto = MapToDto(payment);
            return ApiResponse<PaymentDto>.SuccessResponse(dto, "Payment processed successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error processing payment");
            return ApiResponse<PaymentDto>.FailureResponse("An error occurred while processing the payment");
        }
    }

    public async Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var payment = await _paymentRepository.Query()
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id && p.Invoice.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (payment == null)
            {
                return ApiResponse<PaymentDto>.FailureResponse("Payment not found");
            }

            var dto = MapToDto(payment);
            return ApiResponse<PaymentDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment. PaymentId={PaymentId}", id);
            return ApiResponse<PaymentDto>.FailureResponse("An error occurred while retrieving the payment");
        }
    }

    public async Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var invoice = await _invoiceRepository.Query()
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.FacilityId == facilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                return ApiResponse<IEnumerable<PaymentDto>>.FailureResponse("Invoice not found");
            }

            var payments = await _paymentRepository.Query()
                .Where(p => p.InvoiceId == invoiceId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            var dtos = payments.Select(MapToDto);
            return ApiResponse<IEnumerable<PaymentDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for invoice. InvoiceId={InvoiceId}", invoiceId);
            return ApiResponse<IEnumerable<PaymentDto>>.FailureResponse(
                "An error occurred while retrieving payments");
        }
    }

    private static string GeneratePaymentReference()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            PaymentReference = payment.PaymentReference,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),
            PaymentDate = payment.PaymentDate,
            TransactionId = payment.TransactionId
        };
    }
}
