using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Invoices;
using CentralHealth.Application.DTOs.Payments;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IRepository<Invoice> _invoiceRepository;
    private readonly IRepository<Patient> _patientRepository;
    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly IRepository<MedicalService> _medicalServiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IRepository<Invoice> invoiceRepository,
        IRepository<Patient> patientRepository,
        IRepository<Appointment> appointmentRepository,
        IRepository<MedicalService> medicalServiceRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _patientRepository = patientRepository;
        _appointmentRepository = appointmentRepository;
        _medicalServiceRepository = medicalServiceRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating invoice. PatientId={PatientId}, AppointmentId={AppointmentId}, ItemsCount={ItemsCount}, FacilityId={FacilityId}",
                request.PatientId, request.AppointmentId, request.Items.Count, request.FacilityId);

            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<InvoiceDto>.FailureResponse(errors);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var patient = await _patientRepository.Query()
                .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.FacilityId == request.FacilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found. PatientId={PatientId}", request.PatientId);
                return ApiResponse<InvoiceDto>.FailureResponse("Patient not found");
            }

            Appointment? appointment = null;
            if (request.AppointmentId.HasValue)
            {
                appointment = await _appointmentRepository.Query()
                    .FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value && a.FacilityId == request.FacilityId && !a.IsDeleted, cancellationToken);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment not found. AppointmentId={AppointmentId}", request.AppointmentId);
                    return ApiResponse<InvoiceDto>.FailureResponse("Appointment not found");
                }
            }

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = GenerateInvoiceNumber(),
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                PatientId = request.PatientId,
                AppointmentId = request.AppointmentId,
                FacilityId = request.FacilityId,
                DiscountPercentage = request.DiscountPercentage,
                Notes = request.Notes,
                Status = InvoiceStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.Username,
                Items = new List<InvoiceItem>()
            };

            decimal subTotal = 0;

            foreach (var itemRequest in request.Items)
            {
                MedicalService? service = null;
                if (itemRequest.MedicalServiceId.HasValue)
                {
                    service = await _medicalServiceRepository.GetByIdAsync(itemRequest.MedicalServiceId.Value, cancellationToken);
                }

                var itemTotal = (itemRequest.Quantity * itemRequest.UnitPrice) - itemRequest.DiscountAmount;
                
                var invoiceItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    MedicalServiceId = itemRequest.MedicalServiceId,
                    Description = service?.Name ?? itemRequest.Description,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    DiscountAmount = itemRequest.DiscountAmount,
                    TotalPrice = itemTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.Username
                };

                invoice.Items.Add(invoiceItem);
                subTotal += itemTotal;
            }

            invoice.SubTotal = subTotal;
            invoice.DiscountAmount = subTotal * (request.DiscountPercentage / 100);
            invoice.TotalAmount = subTotal - invoice.DiscountAmount;
            invoice.Currency = "NGN";

            await _invoiceRepository.AddAsync(invoice, cancellationToken);

            if (appointment != null && appointment.Status == AppointmentStatus.CheckedIn)
            {
                var previousStatus = appointment.Status;
                appointment.Status = AppointmentStatus.AwaitingPayment;
                appointment.UpdatedAt = DateTime.UtcNow;
                appointment.UpdatedBy = request.Username;
                _appointmentRepository.Update(appointment);
                
                _logger.LogInformation(
                    "Appointment status changed after invoice creation. AppointmentId={AppointmentId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}",
                    appointment.Id, previousStatus, appointment.Status);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Invoice created successfully. InvoiceId={InvoiceId}, InvoiceNumber={InvoiceNumber}, PatientId={PatientId}, TotalAmount={TotalAmount}, CreatedBy={CreatedBy}",
                invoice.Id, invoice.InvoiceNumber, invoice.PatientId, invoice.TotalAmount, request.Username);

            var dto = MapToDto(invoice, patient);
            return ApiResponse<InvoiceDto>.SuccessResponse(dto, "Invoice created successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating invoice");
            return ApiResponse<InvoiceDto>.FailureResponse("An error occurred while creating the invoice");
        }
    }

    public async Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _invoiceRepository.Query()
                .Include(i => i.Patient)
                .Include(i => i.Items)
                    .ThenInclude(item => item.MedicalService)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id && i.FacilityId == facilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found. InvoiceId={InvoiceId}", id);
                return ApiResponse<InvoiceDto>.FailureResponse("Invoice not found");
            }

            _logger.LogInformation("Invoice retrieved. InvoiceId={InvoiceId}, InvoiceNumber={InvoiceNumber}", id, invoice.InvoiceNumber);

            var dto = MapToDto(invoice, invoice.Patient);
            return ApiResponse<InvoiceDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice. InvoiceId={InvoiceId}", id);
            return ApiResponse<InvoiceDto>.FailureResponse("An error occurred while retrieving the invoice");
        }
    }

    public async Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        GetInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading invoices list. FacilityId={FacilityId}, Filters: PatientId={PatientId}, StartDate={StartDate}, EndDate={EndDate}",
                request.FacilityId, request.PatientId, request.StartDate, request.EndDate);

            var query = _invoiceRepository.Query()
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Where(i => i.FacilityId == request.FacilityId && !i.IsDeleted);

            if (request.PatientId.HasValue)
            {
                query = query.Where(i => i.PatientId == request.PatientId.Value);
                _logger.LogInformation("Filter applied: PatientId={PatientId}", request.PatientId);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= request.StartDate.Value.Date);
                _logger.LogInformation("Filter applied: StartDate={StartDate}", request.StartDate);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= request.EndDate.Value.Date);
                _logger.LogInformation("Filter applied: EndDate={EndDate}", request.EndDate);
            }

            query = query.OrderByDescending(i => i.InvoiceDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = invoices.Select(i => MapToDto(i, i.Patient));

            var result = PagedResult<InvoiceDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);

            _logger.LogInformation(
                "Invoices list loaded. TotalCount={TotalCount}, PageNumber={PageNumber}, PageSize={PageSize}",
                totalCount, request.PageNumber, request.PageSize);

            return ApiResponse<PagedResult<InvoiceDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return ApiResponse<PagedResult<InvoiceDto>>.FailureResponse(
                "An error occurred while retrieving invoices");
        }
    }

    public async Task<ApiResponse<bool>> CancelInvoiceAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _invoiceRepository.Query()
                .FirstOrDefaultAsync(i => i.Id == id && i.FacilityId == facilityId && !i.IsDeleted, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for cancellation. InvoiceId={InvoiceId}", id);
                return ApiResponse<bool>.FailureResponse("Invoice not found");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                _logger.LogWarning("Cannot cancel paid invoice. InvoiceId={InvoiceId}", id);
                return ApiResponse<bool>.FailureResponse("Cannot cancel a paid invoice");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ApiResponse<bool>.FailureResponse("Invoice is already cancelled");

            var previousStatus = invoice.Status;
            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = username;

            _invoiceRepository.Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Invoice cancelled. InvoiceId={InvoiceId}, InvoiceNumber={InvoiceNumber}, CancelledBy={CancelledBy}",
                id, invoice.InvoiceNumber, username);

            return ApiResponse<bool>.SuccessResponse(true, "Invoice cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice. InvoiceId={InvoiceId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while cancelling the invoice");
        }
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static InvoiceDto MapToDto(Invoice invoice, Patient patient)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            PatientId = invoice.PatientId,
            PatientName = patient.FullName,
            PatientCode = patient.PatientCode,
            AppointmentId = invoice.AppointmentId,
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            DiscountPercentage = invoice.DiscountPercentage,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            OutstandingAmount = invoice.OutstandingAmount,
            Currency = invoice.Currency,
            Status = invoice.Status.ToString(),
            Notes = invoice.Notes,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                MedicalServiceId = item.MedicalServiceId,
                ServiceName = item.MedicalService?.Name,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TotalPrice = item.TotalPrice
            }).ToList(),
            Payments = invoice.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentReference = p.PaymentReference,
                Amount = p.Amount,
                Currency = p.Currency,
                Method = p.Method.ToString(),
                Status = p.Status.ToString(),
                PaymentDate = p.PaymentDate,
                TransactionId = p.TransactionId
            }).ToList(),
            CreatedAt = invoice.CreatedAt
        };
    }
}
