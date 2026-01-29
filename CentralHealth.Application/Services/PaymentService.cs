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
    private readonly IValidationService _validationService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IRepository<Payment> paymentRepository,
        IRepository<Invoice> invoiceRepository,
        IRepository<Appointment> appointmentRepository,
        IRepository<PatientWallet> walletRepository,
        IRepository<WalletTransaction> walletTransactionRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _appointmentRepository = appointmentRepository;
        _walletRepository = walletRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment. InvoiceId={InvoiceId}, Amount={Amount}, Method={Method}, FacilityId={FacilityId}",
                request.InvoiceId, request.Amount, request.Method, request.FacilityId);

            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<PaymentDto>.FailureResponse(errors);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var invoice = await _invoiceRepository.Query()
                .Include(i => i.Appointment)
                .Include(i => i.Patient)
                    .ThenInclude(p => p.Wallet)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.FacilityId == request.FacilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for payment. InvoiceId={InvoiceId}", request.InvoiceId);
                return ApiResponse<PaymentDto>.FailureResponse("Invoice not found");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                _logger.LogWarning("Attempt to pay already paid invoice. InvoiceId={InvoiceId}", request.InvoiceId);
                return ApiResponse<PaymentDto>.FailureResponse("Invoice is already paid");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                _logger.LogWarning("Attempt to pay cancelled invoice. InvoiceId={InvoiceId}", request.InvoiceId);
                return ApiResponse<PaymentDto>.FailureResponse("Cannot process payment for a cancelled invoice");
            }

            if (request.Amount > invoice.OutstandingAmount)
            {
                _logger.LogWarning(
                    "Payment amount exceeds outstanding. InvoiceId={InvoiceId}, PaymentAmount={PaymentAmount}, OutstandingAmount={OutstandingAmount}",
                    request.InvoiceId, request.Amount, invoice.OutstandingAmount);
                return ApiResponse<PaymentDto>.FailureResponse(
                    $"Payment amount ({request.Amount:N2}) exceeds outstanding amount ({invoice.OutstandingAmount:N2})");
            }

            if (request.Method == PaymentMethod.Wallet)
            {
                var wallet = invoice.Patient.Wallet;
                if (wallet == null || wallet.Balance < request.Amount)
                {
                    _logger.LogWarning(
                        "Insufficient wallet balance. PatientId={PatientId}, WalletBalance={WalletBalance}, PaymentAmount={PaymentAmount}",
                        invoice.PatientId, wallet?.Balance ?? 0, request.Amount);
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
                    CreatedBy = request.Username
                };

                await _walletTransactionRepository.AddAsync(walletTransaction, cancellationToken);

                _logger.LogInformation(
                    "Wallet payment deducted. WalletId={WalletId}, BalanceBefore={BalanceBefore}, BalanceAfter={BalanceAfter}",
                    wallet.Id, balanceBefore, wallet.Balance);
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
                ProcessedByUserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.Username
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            var previousInvoiceStatus = invoice.Status;
            invoice.PaidAmount += request.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = request.Username;

            _invoiceRepository.Update(invoice);

            _logger.LogInformation(
                "Invoice status updated. InvoiceId={InvoiceId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, PaidAmount={PaidAmount}, TotalAmount={TotalAmount}",
                invoice.Id, previousInvoiceStatus, invoice.Status, invoice.PaidAmount, invoice.TotalAmount);

            if (invoice.Status == InvoiceStatus.Paid && invoice.Appointment != null)
            {
                var appointment = invoice.Appointment;
                if (appointment.Status == AppointmentStatus.AwaitingPayment)
                {
                    var previousAppointmentStatus = appointment.Status;
                    appointment.Status = AppointmentStatus.AwaitingVitals;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    appointment.UpdatedBy = request.Username;
                    _appointmentRepository.Update(appointment);

                    _logger.LogInformation(
                        "Patient moved to AwaitingVitals after payment. AppointmentId={AppointmentId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, PatientId={PatientId}",
                        appointment.Id, previousAppointmentStatus, appointment.Status, appointment.PatientId);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Payment processed successfully. PaymentId={PaymentId}, PaymentReference={PaymentReference}, InvoiceId={InvoiceId}, Amount={Amount}, ProcessedBy={ProcessedBy}",
                payment.Id, payment.PaymentReference, invoice.Id, request.Amount, request.Username);

            var dto = MapToDto(payment);
            return ApiResponse<PaymentDto>.SuccessResponse(dto, "Payment processed successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error processing payment. InvoiceId={InvoiceId}", request.InvoiceId);
            return ApiResponse<PaymentDto>.FailureResponse("An error occurred while processing the payment");
        }
    }

    public async Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.Query()
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id && p.Invoice.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found. PaymentId={PaymentId}", id);
                return ApiResponse<PaymentDto>.FailureResponse("Payment not found");
            }

            _logger.LogInformation("Payment retrieved. PaymentId={PaymentId}, PaymentReference={PaymentReference}", id, payment.PaymentReference);

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
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading payments for invoice. InvoiceId={InvoiceId}", invoiceId);

            var invoice = await _invoiceRepository.Query()
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.FacilityId == facilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for payments retrieval. InvoiceId={InvoiceId}", invoiceId);
                return ApiResponse<IEnumerable<PaymentDto>>.FailureResponse("Invoice not found");
            }

            var payments = await _paymentRepository.Query()
                .Where(p => p.InvoiceId == invoiceId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Payments loaded for invoice. InvoiceId={InvoiceId}, PaymentsCount={PaymentsCount}", invoiceId, payments.Count);

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
