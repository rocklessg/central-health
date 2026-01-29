using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Users;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Facility> _facilityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IRepository<User> userRepository,
        IRepository<Facility> facilityRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _facilityRepository = facilityRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<UserDto>.FailureResponse(errors);

            var usernameExists = await _userRepository.Query()
                .AnyAsync(u => u.Username == request.NewUsername && !u.IsDeleted, cancellationToken);

            if (usernameExists)
                return ApiResponse<UserDto>.FailureResponse("Username already exists");

            var emailExists = await _userRepository.Query()
                .AnyAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

            if (emailExists)
                return ApiResponse<UserDto>.FailureResponse("Email already exists");

            var facility = await _facilityRepository.GetByIdAsync(request.FacilityId, cancellationToken);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FacilityId = request.FacilityId,
                Username = request.NewUsername,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.Username
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User created. UserId={UserId}, Username={Username}, CreatedBy={CreatedBy}", 
                user.Id, user.Username, request.Username);

            return ApiResponse<UserDto>.SuccessResponse(MapToDto(user, facility?.Name ?? ""), "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ApiResponse<UserDto>.FailureResponse("An error occurred while creating the user");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(
        Guid id,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.Query()
                .Include(u => u.Facility)
                .FirstOrDefaultAsync(u => u.Id == id && u.FacilityId == facilityId && !u.IsDeleted, cancellationToken);

            if (user == null)
                return ApiResponse<UserDto>.FailureResponse("User not found");

            _logger.LogInformation("User retrieved. UserId={UserId}", id);

            return ApiResponse<UserDto>.SuccessResponse(MapToDto(user, user.Facility.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user. UserId={UserId}", id);
            return ApiResponse<UserDto>.FailureResponse("An error occurred while retrieving the user");
        }
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading users list. FacilityId={FacilityId}, SearchTerm={SearchTerm}", 
                request.FacilityId, request.SearchTerm);

            var query = _userRepository.Query()
                .Include(u => u.Facility)
                .Where(u => u.FacilityId == request.FacilityId && !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
                
                _logger.LogInformation("Search executed: SearchTerm={SearchTerm}", request.SearchTerm);
            }

            if (request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
                _logger.LogInformation("Filter applied: Role={Role}", request.Role);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
                _logger.LogInformation("Filter applied: IsActive={IsActive}", request.IsActive);
            }

            query = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = users.Select(u => MapToDto(u, u.Facility.Name));

            var result = PagedResult<UserDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);

            _logger.LogInformation("Users list loaded. TotalCount={TotalCount}, PageNumber={PageNumber}", 
                totalCount, request.PageNumber);

            return ApiResponse<PagedResult<UserDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return ApiResponse<PagedResult<UserDto>>.FailureResponse("An error occurred while retrieving users");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<UserDto>.FailureResponse(errors);

            var user = await _userRepository.Query()
                .Include(u => u.Facility)
                .FirstOrDefaultAsync(u => u.Id == id && u.FacilityId == request.FacilityId && !u.IsDeleted, cancellationToken);

            if (user == null)
                return ApiResponse<UserDto>.FailureResponse("User not found");

            var emailExists = await _userRepository.Query()
                .AnyAsync(u => u.Email == request.Email && u.Id != id && !u.IsDeleted, cancellationToken);

            if (emailExists)
                return ApiResponse<UserDto>.FailureResponse("Email already exists");

            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = request.Username;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User updated. UserId={UserId}, UpdatedBy={UpdatedBy}", id, request.Username);

            return ApiResponse<UserDto>.SuccessResponse(MapToDto(user, user.Facility.Name), "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user. UserId={UserId}", id);
            return ApiResponse<UserDto>.FailureResponse("An error occurred while updating the user");
        }
    }

    public async Task<ApiResponse<bool>> DeactivateUserAsync(
        Guid id,
        Guid facilityId,
        string username,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.Id == id && u.FacilityId == facilityId && !u.IsDeleted, cancellationToken);

            if (user == null)
                return ApiResponse<bool>.FailureResponse("User not found");

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = username;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User deactivated. UserId={UserId}, DeactivatedBy={DeactivatedBy}", id, username);

            return ApiResponse<bool>.SuccessResponse(true, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user. UserId={UserId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while deactivating the user");
        }
    }

    private static UserDto MapToDto(User user, string facilityName)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            FacilityId = user.FacilityId,
            FacilityName = facilityName,
            CreatedAt = user.CreatedAt
        };
    }
}
