using CentralHealth.Application.DTOs.MedicalServices;
using FluentValidation;

namespace CentralHealth.Application.Validators;

public class CreateMedicalServiceRequestValidator : AbstractValidator<CreateMedicalServiceRequest>
{
    public CreateMedicalServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(200).WithMessage("Service name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Service code is required")
            .MaximumLength(50).WithMessage("Service code cannot exceed 50 characters");

        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("Clinic is required");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

public class UpdateMedicalServiceRequestValidator : AbstractValidator<UpdateMedicalServiceRequest>
{
    public UpdateMedicalServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(200).WithMessage("Service name cannot exceed 200 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
    }
}
