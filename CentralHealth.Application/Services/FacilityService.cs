using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class FacilityService : IFacilityService
{
    private readonly IRepository<Facility> _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly ILogger<FacilityService> _logger;

    public FacilityService(
        IRepository<Facility> facilityRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<FacilityService> logger)
    {
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<FacilityDto>> CreateFacilityAsync(
        CreateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<FacilityDto>.FailureResponse(errors);

            var codeExists = await _facilityRepository.Query()
                .AnyAsync(f => f.Code == request.Code && !f.IsDeleted, cancellationToken);

            if (codeExists)
                return ApiResponse<FacilityDto>.FailureResponse("Facility code already exists");

            var facility = new Facility
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };

            await _facilityRepository.AddAsync(facility, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Facility created. FacilityId={FacilityId}, Code={Code}",
                facility.Id, facility.Code);

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility), "Facility created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating facility");
            return ApiResponse<FacilityDto>.FailureResponse("An error occurred while creating the facility");
        }
    }

    public async Task<ApiResponse<FacilityDto>> GetFacilityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facility = await _facilityRepository.Query()
                .Include(f => f.Clinics.Where(c => !c.IsDeleted))
                .Include(f => f.Users.Where(u => !u.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

            if (facility == null)
                return ApiResponse<FacilityDto>.FailureResponse("Facility not found");

            _logger.LogInformation("Facility retrieved. FacilityId={FacilityId}", id);

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facility. FacilityId={FacilityId}", id);
            return ApiResponse<FacilityDto>.FailureResponse("An error occurred while retrieving the facility");
        }
    }

    public async Task<ApiResponse<IEnumerable<FacilityDto>>> GetAllFacilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilities = await _facilityRepository.Query()
                .Include(f => f.Clinics.Where(c => !c.IsDeleted))
                .Include(f => f.Users.Where(u => !u.IsDeleted))
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Facilities list retrieved. Count={Count}", facilities.Count);

            var dtos = facilities.Select(MapToDto);
            return ApiResponse<IEnumerable<FacilityDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facilities");
            return ApiResponse<IEnumerable<FacilityDto>>.FailureResponse("An error occurred while retrieving facilities");
        }
    }

    public async Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(
        Guid id,
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<FacilityDto>.FailureResponse(errors);

            var facility = await _facilityRepository.Query()
                .Include(f => f.Clinics.Where(c => !c.IsDeleted))
                .Include(f => f.Users.Where(u => !u.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

            if (facility == null)
                return ApiResponse<FacilityDto>.FailureResponse("Facility not found");

            facility.Name = request.Name;
            facility.Address = request.Address;
            facility.Phone = request.Phone;
            facility.Email = request.Email;
            facility.UpdatedAt = DateTime.UtcNow;
            facility.UpdatedBy = "system";

            _facilityRepository.Update(facility);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Facility updated. FacilityId={FacilityId}", id);

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility), "Facility updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facility. FacilityId={FacilityId}", id);
            return ApiResponse<FacilityDto>.FailureResponse("An error occurred while updating the facility");
        }
    }

    private static FacilityDto MapToDto(Facility facility)
    {
        return new FacilityDto
        {
            Id = facility.Id,
            Name = facility.Name,
            Code = facility.Code,
            Address = facility.Address,
            Phone = facility.Phone,
            Email = facility.Email,
            IsActive = facility.IsActive,
            ClinicsCount = facility.Clinics?.Count ?? 0,
            UsersCount = facility.Users?.Count ?? 0,
            CreatedAt = facility.CreatedAt
        };
    }
}
