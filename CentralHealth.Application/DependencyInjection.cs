using CentralHealth.Application.Interfaces;
using CentralHealth.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CentralHealth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IRecordsService, RecordsService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IMedicalServiceService, MedicalServiceService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
