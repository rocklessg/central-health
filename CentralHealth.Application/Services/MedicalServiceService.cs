using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.MedicalServices;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class MedicalServiceService : IMedicalServiceService
{
    private readonly IRepository<MedicalService> _medicalServiceRepository;
    private readonly IRepository<Clinic> _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly ILogger<MedicalServiceService> _logger;

    public MedicalServiceService(
        IRepository<MedicalService> medicalServiceRepository,
        IRepository<Clinic> clinicRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<MedicalServiceService> logger)
    {
        _medicalServiceRepository = medicalServiceRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<MedicalServiceDto>> CreateMedicalServiceAsync(
        CreateMedicalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<MedicalServiceDto>.FailureResponse(errors);

            var clinic = await _clinicRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == request.ClinicId && c.FacilityId == request.FacilityId && !c.IsDeleted, cancellationToken);

            if (clinic == null)
                return ApiResponse<MedicalServiceDto>.FailureResponse("Clinic not found");

            var codeExists = await _medicalServiceRepository.Query()
                .AnyAsync(s => s.ClinicId == request.ClinicId && s.Code == request.Code && !s.IsDeleted, cancellationToken);

            if (codeExists)
                return ApiResponse<MedicalServiceDto>.FailureResponse("Service code already exists in this clinic");

            var service = new MedicalService
            {
                Id = Guid.NewGuid(),
                ClinicId = request.ClinicId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                UnitPrice = request.UnitPrice,
                Currency = "NGN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.Username
            };

            await _medicalServiceRepository.AddAsync(service, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Medical service created. ServiceId={ServiceId}, Code={Code}, CreatedBy={CreatedBy}", 
                service.Id, service.Code, request.Username);

            return ApiResponse<MedicalServiceDto>.SuccessResponse(MapToDto(service, clinic.Name), "Medical service created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating medical service");
            return ApiResponse<MedicalServiceDto>.FailureResponse("An error occurred while creating the medical service");
        }
    }

    public async Task<ApiResponse<MedicalServiceDto>> GetMedicalServiceByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _medicalServiceRepository.Query()
                .Include(s => s.Clinic)
                .FirstOrDefaultAsync(s => s.Id == id && s.Clinic.FacilityId == facilityId && !s.IsDeleted, cancellationToken);

            if (service == null)
                return ApiResponse<MedicalServiceDto>.FailureResponse("Medical service not found");

            _logger.LogInformation("Medical service retrieved. ServiceId={ServiceId}", id);

            return ApiResponse<MedicalServiceDto>.SuccessResponse(MapToDto(service, service.Clinic.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical service. ServiceId={ServiceId}", id);
            return ApiResponse<MedicalServiceDto>.FailureResponse("An error occurred while retrieving the medical service");
        }
    }

    public async Task<ApiResponse<PagedResult<MedicalServiceDto>>> GetMedicalServicesAsync(
        GetMedicalServicesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading medical services list. FacilityId={FacilityId}, ClinicId={ClinicId}, SearchTerm={SearchTerm}", 
                request.FacilityId, request.ClinicId, request.SearchTerm);

            var query = _medicalServiceRepository.Query()
                .Include(s => s.Clinic)
                .Where(s => s.Clinic.FacilityId == request.FacilityId && !s.IsDeleted);

            if (request.ClinicId.HasValue)
            {
                query = query.Where(s => s.ClinicId == request.ClinicId.Value);
                _logger.LogInformation("Filter applied: ClinicId={ClinicId}", request.ClinicId);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.Code.ToLower().Contains(searchTerm));
                
                _logger.LogInformation("Search executed: SearchTerm={SearchTerm}", request.SearchTerm);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == request.IsActive.Value);
                _logger.LogInformation("Filter applied: IsActive={IsActive}", request.IsActive);
            }

            query = query.OrderBy(s => s.Clinic.Name).ThenBy(s => s.Name);

            var totalCount = await query.CountAsync(cancellationToken);

            var services = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = services.Select(s => MapToDto(s, s.Clinic.Name));

            var result = PagedResult<MedicalServiceDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);

            _logger.LogInformation("Medical services list loaded. TotalCount={TotalCount}, PageNumber={PageNumber}", 
                totalCount, request.PageNumber);

            return ApiResponse<PagedResult<MedicalServiceDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medical services");
            return ApiResponse<PagedResult<MedicalServiceDto>>.FailureResponse("An error occurred while retrieving medical services");
        }
    }

    public async Task<ApiResponse<MedicalServiceDto>> UpdateMedicalServiceAsync(
        Guid id,
        UpdateMedicalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<MedicalServiceDto>.FailureResponse(errors);

            var service = await _medicalServiceRepository.Query()
                .Include(s => s.Clinic)
                .FirstOrDefaultAsync(s => s.Id == id && s.Clinic.FacilityId == request.FacilityId && !s.IsDeleted, cancellationToken);

            if (service == null)
                return ApiResponse<MedicalServiceDto>.FailureResponse("Medical service not found");

            service.Name = request.Name;
            service.Description = request.Description;
            service.UnitPrice = request.UnitPrice;
            service.IsActive = request.IsActive;
            service.UpdatedAt = DateTime.UtcNow;
            service.UpdatedBy = request.Username;

            _medicalServiceRepository.Update(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Medical service updated. ServiceId={ServiceId}, UpdatedBy={UpdatedBy}", id, request.Username);

            return ApiResponse<MedicalServiceDto>.SuccessResponse(MapToDto(service, service.Clinic.Name), "Medical service updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medical service. ServiceId={ServiceId}", id);
            return ApiResponse<MedicalServiceDto>.FailureResponse("An error occurred while updating the medical service");
        }
    }

    public async Task<ApiResponse<bool>> DeleteMedicalServiceAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _medicalServiceRepository.Query()
                .Include(s => s.Clinic)
                .FirstOrDefaultAsync(s => s.Id == id && s.Clinic.FacilityId == facilityId && !s.IsDeleted, cancellationToken);

            if (service == null)
                return ApiResponse<bool>.FailureResponse("Medical service not found");

            service.IsDeleted = true;
            service.UpdatedAt = DateTime.UtcNow;
            service.UpdatedBy = username;

            _medicalServiceRepository.Update(service);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Medical service deleted. ServiceId={ServiceId}, DeletedBy={DeletedBy}", id, username);

            return ApiResponse<bool>.SuccessResponse(true, "Medical service deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting medical service. ServiceId={ServiceId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while deleting the medical service");
        }
    }

    private static MedicalServiceDto MapToDto(MedicalService service, string clinicName)
    {
        return new MedicalServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Code = service.Code,
            Description = service.Description,
            UnitPrice = service.UnitPrice,
            Currency = service.Currency,
            IsActive = service.IsActive,
            ClinicId = service.ClinicId,
            ClinicName = clinicName,
            CreatedAt = service.CreatedAt
        };
    }
}
