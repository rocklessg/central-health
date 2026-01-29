using CentralHealth.Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CentralHealth.Application.Services;

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateAsync<T>(T request, CancellationToken cancellationToken = default)
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        
        if (validator == null)
            return (true, new List<string>());

        var result = await validator.ValidateAsync(request, cancellationToken);
        
        if (result.IsValid)
            return (true, new List<string>());

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return (false, errors);
    }
}
