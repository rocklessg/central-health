using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Appointments;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly IRepository<Patient> _patientRepository;
    private readonly IRepository<Clinic> _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidationService _validationService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IRepository<Appointment> appointmentRepository,
        IRepository<Patient> patientRepository,
        IRepository<Clinic> clinicRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidationService validationService,
        ILogger<AppointmentService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<AppointmentDto>> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<AppointmentDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var patient = await _patientRepository.Query()
                .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found. PatientId={PatientId}", request.PatientId);
                return ApiResponse<AppointmentDto>.FailureResponse("Patient not found");
            }

            var clinic = await _clinicRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == request.ClinicId && c.FacilityId == facilityId && !c.IsDeleted, cancellationToken);

            if (clinic == null)
            {
                _logger.LogWarning("Clinic not found. ClinicId={ClinicId}", request.ClinicId);
                return ApiResponse<AppointmentDto>.FailureResponse("Clinic not found");
            }

            var hasConflict = await _appointmentRepository.Query()
                .AnyAsync(a =>
                    a.PatientId == request.PatientId &&
                    a.AppointmentDate == request.AppointmentDate.Date &&
                    a.AppointmentTime == request.AppointmentTime &&
                    a.Status != AppointmentStatus.Cancelled &&
                    !a.IsDeleted,
                    cancellationToken);

            if (hasConflict)
            {
                _logger.LogWarning(
                    "Appointment conflict detected. PatientId={PatientId}, Date={Date}, Time={Time}",
                    request.PatientId, request.AppointmentDate, request.AppointmentTime);
                return ApiResponse<AppointmentDto>.FailureResponse(
                    "Patient already has an appointment at this date and time");
            }

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                ClinicId = request.ClinicId,
                FacilityId = facilityId,
                AppointmentDate = request.AppointmentDate.Date,
                AppointmentTime = request.AppointmentTime,
                Type = request.Type,
                Status = AppointmentStatus.Scheduled,
                ReasonForVisit = request.ReasonForVisit,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            await _appointmentRepository.AddAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Appointment created successfully. AppointmentId={AppointmentId}, PatientId={PatientId}, ClinicId={ClinicId}",
                appointment.Id, appointment.PatientId, appointment.ClinicId);

            var dto = MapToDto(appointment, patient, clinic);
            return ApiResponse<AppointmentDto>.SuccessResponse(dto, "Appointment created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return ApiResponse<AppointmentDto>.FailureResponse("An error occurred while creating the appointment");
        }
    }

    public async Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var appointment = await _appointmentRepository.Query()
                .Include(a => a.Patient)
                .Include(a => a.Clinic)
                .FirstOrDefaultAsync(a => a.Id == id && a.FacilityId == facilityId && !a.IsDeleted, cancellationToken);

            if (appointment == null)
            {
                return ApiResponse<AppointmentDto>.FailureResponse("Appointment not found");
            }

            var dto = MapToDto(appointment, appointment.Patient, appointment.Clinic);
            return ApiResponse<AppointmentDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointment. AppointmentId={AppointmentId}", id);
            return ApiResponse<AppointmentDto>.FailureResponse("An error occurred while retrieving the appointment");
        }
    }

    public async Task<ApiResponse<PagedResult<AppointmentDto>>> GetAppointmentsAsync(
        GetAppointmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var query = _appointmentRepository.Query()
                .Include(a => a.Patient)
                .Include(a => a.Clinic)
                .Where(a => a.FacilityId == facilityId && !a.IsDeleted);

            if (request.StartDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= request.StartDate.Value.Date);

            if (request.EndDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= request.EndDate.Value.Date);

            if (request.ClinicId.HasValue)
                query = query.Where(a => a.ClinicId == request.ClinicId.Value);

            query = query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime);

            var totalCount = await query.CountAsync(cancellationToken);

            var appointments = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = appointments.Select(a => MapToDto(a, a.Patient, a.Clinic));

            var result = PagedResult<AppointmentDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ApiResponse<PagedResult<AppointmentDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments");
            return ApiResponse<PagedResult<AppointmentDto>>.FailureResponse(
                "An error occurred while retrieving appointments");
        }
    }

    public async Task<ApiResponse<bool>> CancelAppointmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var appointment = await _appointmentRepository.Query()
                .FirstOrDefaultAsync(a => a.Id == id && a.FacilityId == facilityId && !a.IsDeleted, cancellationToken);

            if (appointment == null)
            {
                return ApiResponse<bool>.FailureResponse("Appointment not found");
            }

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return ApiResponse<bool>.FailureResponse("Appointment is already cancelled");
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return ApiResponse<bool>.FailureResponse("Cannot cancel a completed appointment");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;
            appointment.UpdatedBy = _currentUserService.Username;

            _appointmentRepository.Update(appointment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Appointment cancelled. AppointmentId={AppointmentId}", id);

            return ApiResponse<bool>.SuccessResponse(true, "Appointment cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment. AppointmentId={AppointmentId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while cancelling the appointment");
        }
    }

    private static AppointmentDto MapToDto(Appointment appointment, Patient patient, Clinic clinic)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patient.FullName,
            PatientCode = patient.PatientCode,
            ClinicId = appointment.ClinicId,
            ClinicName = clinic.Name,
            AppointmentDate = appointment.AppointmentDate,
            AppointmentTime = appointment.AppointmentTime,
            AppointmentDateTime = appointment.AppointmentDateTime,
            Type = appointment.Type.ToString(),
            Status = appointment.Status.ToString(),
            ReasonForVisit = appointment.ReasonForVisit,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt
        };
    }
}
