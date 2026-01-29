using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Records;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class RecordsService : IRecordsService
{
    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly ILogger<RecordsService> _logger;

    public RecordsService(
        IRepository<Appointment> appointmentRepository,
        ILogger<RecordsService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<PatientRecordDto>>> GetRecordsAsync(
        RecordsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading records list. FacilityId={FacilityId}, Filters: StartDate={StartDate}, EndDate={EndDate}, ClinicId={ClinicId}, SearchTerm={SearchTerm}",
                request.FacilityId, request.StartDate, request.EndDate, request.ClinicId, request.SearchTerm);

            var query = _appointmentRepository.Query()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.Wallet)
                .Include(a => a.Clinic)
                .Where(a => a.FacilityId == request.FacilityId && !a.IsDeleted);

            var startDate = request.StartDate ?? DateTime.Today;
            var endDate = request.EndDate ?? DateTime.Today.AddDays(1).AddTicks(-1);
            
            query = query.Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate);

            if (request.ClinicId.HasValue)
            {
                query = query.Where(a => a.ClinicId == request.ClinicId.Value);
                _logger.LogInformation("Filter applied: ClinicId={ClinicId}", request.ClinicId);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(a =>
                    a.Patient.FirstName.ToLower().Contains(searchTerm) ||
                    a.Patient.LastName.ToLower().Contains(searchTerm) ||
                    a.Patient.PatientCode.ToLower().Contains(searchTerm) ||
                    (a.Patient.Phone != null && a.Patient.Phone.Contains(searchTerm)));
                
                _logger.LogInformation("Search executed: SearchTerm={SearchTerm}", request.SearchTerm);
            }

            query = request.SortDescending
                ? query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime)
                : query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime);

            var totalCount = await query.CountAsync(cancellationToken);

            var appointments = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var records = appointments.Select(a => new PatientRecordDto
            {
                AppointmentId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientCode = a.Patient.PatientCode,
                Phone = a.Patient.Phone,
                AppointmentTime = a.AppointmentDateTime,
                Status = a.Status.ToString(),
                ClinicName = a.Clinic.Name,
                ClinicId = a.ClinicId,
                Wallet = new WalletBalanceDto
                {
                    Balance = a.Patient.Wallet?.Balance ?? 0,
                    Currency = a.Patient.Wallet?.Currency ?? "NGN"
                }
            });

            var result = PagedResult<PatientRecordDto>.Create(
                records, request.PageNumber, request.PageSize, totalCount);

            _logger.LogInformation(
                "Records list loaded successfully. TotalCount={TotalCount}, PageNumber={PageNumber}, PageSize={PageSize}",
                totalCount, request.PageNumber, request.PageSize);

            return ApiResponse<PagedResult<PatientRecordDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading records list");
            return ApiResponse<PagedResult<PatientRecordDto>>.FailureResponse(
                "An error occurred while loading records");
        }
    }
}
