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
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidationService _validationService;
    private readonly ILogger<FacilityService> _logger;

    public FacilityService(
        IRepository<Facility> facilityRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidationService validationService,
        ILogger<FacilityService> logger)
    {
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<FacilityDto>> GetCurrentFacilityAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var facility = await _facilityRepository.Query()
                .Include(f => f.Clinics.Where(c => !c.IsDeleted))
                .Include(f => f.Users.Where(u => !u.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == facilityId && !f.IsDeleted, cancellationToken);

            if (facility == null)
                return ApiResponse<FacilityDto>.FailureResponse("Facility not found");

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facility");
            return ApiResponse<FacilityDto>.FailureResponse("An error occurred while retrieving the facility");
        }
    }

    public async Task<ApiResponse<FacilityDto>> UpdateFacilityAsync(
        UpdateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<FacilityDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var facility = await _facilityRepository.Query()
                .Include(f => f.Clinics.Where(c => !c.IsDeleted))
                .Include(f => f.Users.Where(u => !u.IsDeleted))
                .FirstOrDefaultAsync(f => f.Id == facilityId && !f.IsDeleted, cancellationToken);

            if (facility == null)
                return ApiResponse<FacilityDto>.FailureResponse("Facility not found");

            facility.Name = request.Name;
            facility.Address = request.Address;
            facility.Phone = request.Phone;
            facility.Email = request.Email;
            facility.UpdatedAt = DateTime.UtcNow;
            facility.UpdatedBy = _currentUserService.Username;

            _facilityRepository.Update(facility);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Facility updated. FacilityId={FacilityId}", facilityId);

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility), "Facility updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating facility");
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
            ClinicsCount = facility.Clinics.Count,
            UsersCount = facility.Users.Count,
            CreatedAt = facility.CreatedAt
        };
    }
}
