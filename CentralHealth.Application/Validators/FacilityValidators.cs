using CentralHealth.Application.DTOs.Facilities;
using FluentValidation;

namespace CentralHealth.Application.Validators;

public class UpdateFacilityRequestValidator : AbstractValidator<UpdateFacilityRequest>
{
    public UpdateFacilityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Facility name is required")
            .MaximumLength(200).WithMessage("Facility name cannot exceed 200 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters");
    }
}
