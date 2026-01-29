using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Clinics;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class ClinicService : IClinicService
{
    private readonly IRepository<Clinic> _clinicRepository;
    private readonly IRepository<Facility> _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidationService _validationService;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(
        IRepository<Clinic> clinicRepository,
        IRepository<Facility> facilityRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidationService validationService,
        ILogger<ClinicService> logger)
    {
        _clinicRepository = clinicRepository;
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<ClinicDto>> CreateClinicAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<ClinicDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var codeExists = await _clinicRepository.Query()
                .AnyAsync(c => c.FacilityId == facilityId && c.Code == request.Code && !c.IsDeleted, cancellationToken);

            if (codeExists)
                return ApiResponse<ClinicDto>.FailureResponse("Clinic code already exists");

            var facility = await _facilityRepository.GetByIdAsync(facilityId, cancellationToken);

            var clinic = new Clinic
            {
                Id = Guid.NewGuid(),
                FacilityId = facilityId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            await _clinicRepository.AddAsync(clinic, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Clinic created. ClinicId={ClinicId}, Code={Code}", clinic.Id, clinic.Code);

            return ApiResponse<ClinicDto>.SuccessResponse(MapToDto(clinic, facility?.Name ?? ""), "Clinic created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clinic");
            return ApiResponse<ClinicDto>.FailureResponse("An error occurred while creating the clinic");
        }
    }

    public async Task<ApiResponse<ClinicDto>> GetClinicByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var clinic = await _clinicRepository.Query()
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.Id == id && c.FacilityId == facilityId && !c.IsDeleted, cancellationToken);

            if (clinic == null)
                return ApiResponse<ClinicDto>.FailureResponse("Clinic not found");

            return ApiResponse<ClinicDto>.SuccessResponse(MapToDto(clinic, clinic.Facility.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinic. ClinicId={ClinicId}", id);
            return ApiResponse<ClinicDto>.FailureResponse("An error occurred while retrieving the clinic");
        }
    }

    public async Task<ApiResponse<PagedResult<ClinicDto>>> GetClinicsAsync(
        GetClinicsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var query = _clinicRepository.Query()
                .Include(c => c.Facility)
                .Where(c => c.FacilityId == facilityId && !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    c.Code.ToLower().Contains(searchTerm));
            }

            if (request.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.IsActive.Value);

            query = query.OrderBy(c => c.Name);

            var totalCount = await query.CountAsync(cancellationToken);

            var clinics = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = clinics.Select(c => MapToDto(c, c.Facility.Name));

            var result = PagedResult<ClinicDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ApiResponse<PagedResult<ClinicDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clinics");
            return ApiResponse<PagedResult<ClinicDto>>.FailureResponse("An error occurred while retrieving clinics");
        }
    }

    public async Task<ApiResponse<ClinicDto>> UpdateClinicAsync(
        Guid id,
        UpdateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<ClinicDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var clinic = await _clinicRepository.Query()
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.Id == id && c.FacilityId == facilityId && !c.IsDeleted, cancellationToken);

            if (clinic == null)
                return ApiResponse<ClinicDto>.FailureResponse("Clinic not found");

            clinic.Name = request.Name;
            clinic.Description = request.Description;
            clinic.IsActive = request.IsActive;
            clinic.UpdatedAt = DateTime.UtcNow;
            clinic.UpdatedBy = _currentUserService.Username;

            _clinicRepository.Update(clinic);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Clinic updated. ClinicId={ClinicId}", id);

            return ApiResponse<ClinicDto>.SuccessResponse(MapToDto(clinic, clinic.Facility.Name), "Clinic updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating clinic. ClinicId={ClinicId}", id);
            return ApiResponse<ClinicDto>.FailureResponse("An error occurred while updating the clinic");
        }
    }

    public async Task<ApiResponse<bool>> DeleteClinicAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var clinic = await _clinicRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && c.FacilityId == facilityId && !c.IsDeleted, cancellationToken);

            if (clinic == null)
                return ApiResponse<bool>.FailureResponse("Clinic not found");

            clinic.IsDeleted = true;
            clinic.UpdatedAt = DateTime.UtcNow;
            clinic.UpdatedBy = _currentUserService.Username;

            _clinicRepository.Update(clinic);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Clinic deleted. ClinicId={ClinicId}", id);

            return ApiResponse<bool>.SuccessResponse(true, "Clinic deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting clinic. ClinicId={ClinicId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while deleting the clinic");
        }
    }

    private static ClinicDto MapToDto(Clinic clinic, string facilityName)
    {
        return new ClinicDto
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Code = clinic.Code,
            Description = clinic.Description,
            IsActive = clinic.IsActive,
            FacilityId = clinic.FacilityId,
            FacilityName = facilityName,
            CreatedAt = clinic.CreatedAt
        };
    }
}
