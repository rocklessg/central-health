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
    private readonly IValidationService _validationService;
    private readonly ILogger<FacilityService> _logger;

    public FacilityService(
        IRepository<Facility> facilityRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<FacilityService> logger)
    {
        _facilityRepository = facilityRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
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
                .AnyAsync(u => u.Username == request.AdminUsername && !u.IsDeleted, cancellationToken);

            if (usernameExists)
                return ApiResponse<CreateFacilityResponse>.FailureResponse("Admin username already exists");

            var emailExists = await _userRepository.Query()
                .AnyAsync(u => u.Email == request.AdminEmail && !u.IsDeleted, cancellationToken);

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
                Username = request.AdminUsername,
                Email = request.AdminEmail,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                PasswordHash = HashPassword(request.AdminPassword),
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

            _logger.LogInformation("Facility retrieved. FacilityId={FacilityId}", id);

            return ApiResponse<FacilityDto>.SuccessResponse(MapToDto(facility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving facility. FacilityId={FacilityId}", id);
            return ApiResponse<FacilityDto>.FailureResponse("An error occurred while retrieving the facility");
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
