using FluentValidation;

namespace CentralHealth.Application.Interfaces;

public interface IValidationService
{
    Task<(bool IsValid, List<string> Errors)> ValidateAsync<T>(T request, CancellationToken cancellationToken = default);
}
