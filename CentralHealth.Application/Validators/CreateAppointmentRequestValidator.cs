using CentralHealth.Application.DTOs.Appointments;
using FluentValidation;

namespace CentralHealth.Application.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient is required");

        RuleFor(x => x.ClinicId)
            .NotEmpty().WithMessage("Clinic is required");

        RuleFor(x => x.AppointmentDate)
            .NotEmpty().WithMessage("Appointment date is required")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Appointment date cannot be in the past");

        RuleFor(x => x.AppointmentTime)
            .NotEmpty().WithMessage("Appointment time is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid appointment type");
    }
}
