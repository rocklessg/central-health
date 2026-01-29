using CentralHealth.Application.DTOs.Clinics;
using FluentValidation;

namespace CentralHealth.Application.Validators;

public class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Clinic code is required")
            .MaximumLength(50).WithMessage("Clinic code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

public class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(200).WithMessage("Clinic name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}
