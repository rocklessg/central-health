using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Facilities;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using CentralHealth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class FacilityService : IFacilityService
{
    private readonly IRepository<Facility> _facilityRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidationService _validationService;
    private readonly ILogger<FacilityService> _logger;

    public FacilityService(
        IRepository<Facility> facilityRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidationService validationService,
        ILogger<FacilityService> logger)
    {
        _facilityRepository = facilityRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<CreateFacilityResponse>> CreateFacilityAsync(
        CreateFacilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<CreateFacilityResponse>.FailureResponse(errors);

            var codeExists = await _facilityRepository.Query()
                .AnyAsync(f => f.Code == request.Code && !f.IsDeleted, cancellationToken);

            if (codeExists)
                return ApiResponse<CreateFacilityResponse>.FailureResponse("Facility code already exists");

            var usernameExists = await _userRepository.Query()
                .AnyAsync(u => u.Username == request.AdminUser.Username && !u.IsDeleted, cancellationToken);

            if (usernameExists)
                return ApiResponse<CreateFacilityResponse>.FailureResponse("Admin username already exists");

            var emailExists = await _userRepository.Query()
                .AnyAsync(u => u.Email == request.AdminUser.Email && !u.IsDeleted, cancellationToken);

            if (emailExists)
                return ApiResponse<CreateFacilityResponse>.FailureResponse("Admin email already exists");

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

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

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                FacilityId = facility.Id,
                Username = request.AdminUser.Username,
                Email = request.AdminUser.Email,
                FirstName = request.AdminUser.FirstName,
                LastName = request.AdminUser.LastName,
                PasswordHash = HashPassword(request.AdminUser.Password),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            };

            await _userRepository.AddAsync(adminUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Facility created with admin user. FacilityId={FacilityId}, Code={Code}, AdminUserId={AdminUserId}, AdminUsername={AdminUsername}",
                facility.Id, facility.Code, adminUser.Id, adminUser.Username);

            var response = new CreateFacilityResponse
            {
                Facility = MapToDto(facility),
                AdminUser = new AdminUserResponse
                {
                    Id = adminUser.Id,
                    Username = adminUser.Username,
                    Email = adminUser.Email,
                    FullName = adminUser.FullName,
                    Role = adminUser.Role.ToString()
                }
            };

            return ApiResponse<CreateFacilityResponse>.SuccessResponse(response, "Facility created successfully with admin user");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating facility");
            return ApiResponse<CreateFacilityResponse>.FailureResponse("An error occurred while creating the facility");
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

            var dtos = facilities.Select(MapToDto);
            return ApiResponse<IEnumerable<FacilityDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facilities");
            return ApiResponse<IEnumerable<FacilityDto>>.FailureResponse("An error occurred while retrieving facilities");
        }
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

    private static string HashPassword(string password)
    {
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
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
