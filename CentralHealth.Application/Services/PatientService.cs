using CentralHealth.Application.Common;
using CentralHealth.Application.DTOs.Patients;
using CentralHealth.Application.Interfaces;
using CentralHealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentralHealth.Application.Services;

public class PatientService : IPatientService
{
    private readonly IRepository<Patient> _patientRepository;
    private readonly IRepository<PatientWallet> _walletRepository;
    private readonly IRepository<WalletTransaction> _walletTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidationService _validationService;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IRepository<Patient> patientRepository,
        IRepository<PatientWallet> walletRepository,
        IRepository<WalletTransaction> walletTransactionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidationService validationService,
        ILogger<PatientService> logger)
    {
        _patientRepository = patientRepository;
        _walletRepository = walletRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ApiResponse<PatientDto>> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<PatientDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                PatientCode = GeneratePatientCode(),
                FacilityId = facilityId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName,
                Phone = request.Phone,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            await _patientRepository.AddAsync(patient, cancellationToken);

            var wallet = new PatientWallet
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Balance = request.InitialWalletBalance,
                Currency = "NGN",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            await _walletRepository.AddAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Patient created. PatientId={PatientId}, PatientCode={PatientCode}", 
                patient.Id, patient.PatientCode);

            return ApiResponse<PatientDto>.SuccessResponse(MapToDto(patient, wallet), "Patient created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            return ApiResponse<PatientDto>.FailureResponse("An error occurred while creating the patient");
        }
    }

    public async Task<ApiResponse<PatientDto>> GetPatientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var patient = await _patientRepository.Query()
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.Id == id && p.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
                return ApiResponse<PatientDto>.FailureResponse("Patient not found");

            return ApiResponse<PatientDto>.SuccessResponse(MapToDto(patient, patient.Wallet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patient. PatientId={PatientId}", id);
            return ApiResponse<PatientDto>.FailureResponse("An error occurred while retrieving the patient");
        }
    }

    public async Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        GetPatientsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var query = _patientRepository.Query()
                .Include(p => p.Wallet)
                .Where(p => p.FacilityId == facilityId && !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.FirstName.ToLower().Contains(searchTerm) ||
                    p.LastName.ToLower().Contains(searchTerm) ||
                    p.PatientCode.ToLower().Contains(searchTerm) ||
                    (p.Phone != null && p.Phone.Contains(searchTerm)));
            }

            query = query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);

            var totalCount = await query.CountAsync(cancellationToken);

            var patients = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = patients.Select(p => MapToDto(p, p.Wallet));

            var result = PagedResult<PatientDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);
            return ApiResponse<PagedResult<PatientDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients");
            return ApiResponse<PagedResult<PatientDto>>.FailureResponse("An error occurred while retrieving patients");
        }
    }

    public async Task<ApiResponse<PatientDto>> UpdatePatientAsync(
        Guid id,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isValid, errors) = await _validationService.ValidateAsync(request, cancellationToken);
            if (!isValid)
                return ApiResponse<PatientDto>.FailureResponse(errors);

            var facilityId = _currentUserService.FacilityId;

            var patient = await _patientRepository.Query()
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.Id == id && p.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
                return ApiResponse<PatientDto>.FailureResponse("Patient not found");

            patient.FirstName = request.FirstName;
            patient.LastName = request.LastName;
            patient.MiddleName = request.MiddleName;
            patient.Phone = request.Phone;
            patient.Email = request.Email;
            patient.DateOfBirth = request.DateOfBirth;
            patient.Gender = request.Gender;
            patient.Address = request.Address;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = _currentUserService.Username;

            _patientRepository.Update(patient);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Patient updated. PatientId={PatientId}", id);

            return ApiResponse<PatientDto>.SuccessResponse(MapToDto(patient, patient.Wallet), "Patient updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient. PatientId={PatientId}", id);
            return ApiResponse<PatientDto>.FailureResponse("An error occurred while updating the patient");
        }
    }

    public async Task<ApiResponse<bool>> DeletePatientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var facilityId = _currentUserService.FacilityId;

            var patient = await _patientRepository.Query()
                .FirstOrDefaultAsync(p => p.Id == id && p.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
                return ApiResponse<bool>.FailureResponse("Patient not found");

            patient.IsDeleted = true;
            patient.UpdatedAt = DateTime.UtcNow;
            patient.UpdatedBy = _currentUserService.Username;

            _patientRepository.Update(patient);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Patient deleted. PatientId={PatientId}", id);

            return ApiResponse<bool>.SuccessResponse(true, "Patient deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting patient. PatientId={PatientId}", id);
            return ApiResponse<bool>.FailureResponse("An error occurred while deleting the patient");
        }
    }

    public async Task<ApiResponse<bool>> TopUpWalletAsync(
        Guid patientId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (amount <= 0)
                return ApiResponse<bool>.FailureResponse("Amount must be greater than zero");

            var facilityId = _currentUserService.FacilityId;

            var patient = await _patientRepository.Query()
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.Id == patientId && p.FacilityId == facilityId && !p.IsDeleted, cancellationToken);

            if (patient == null)
                return ApiResponse<bool>.FailureResponse("Patient not found");

            var wallet = patient.Wallet;
            if (wallet == null)
                return ApiResponse<bool>.FailureResponse("Patient wallet not found");

            var balanceBefore = wallet.Balance;
            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = amount,
                TransactionType = "TOP_UP",
                Description = "Wallet top-up",
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.Username
            };

            _walletRepository.Update(wallet);
            await _walletTransactionRepository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Wallet topped up. PatientId={PatientId}, Amount={Amount}", patientId, amount);

            return ApiResponse<bool>.SuccessResponse(true, "Wallet topped up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error topping up wallet. PatientId={PatientId}", patientId);
            return ApiResponse<bool>.FailureResponse("An error occurred while topping up the wallet");
        }
    }

    private static string GeneratePatientCode()
    {
        return $"PAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }

    private static PatientDto MapToDto(Patient patient, PatientWallet? wallet)
    {
        return new PatientDto
        {
            Id = patient.Id,
            PatientCode = patient.PatientCode,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            MiddleName = patient.MiddleName,
            FullName = patient.FullName,
            Phone = patient.Phone,
            Email = patient.Email,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            WalletBalance = wallet?.Balance ?? 0,
            WalletCurrency = wallet?.Currency ?? "NGN",
            CreatedAt = patient.CreatedAt
        };
    }
}
