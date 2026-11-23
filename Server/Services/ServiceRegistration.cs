using Server.Services.InterFaces;

namespace Server.Services
{
    /// <summary>Dependency injection registrations for the RevaliNow services.</summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all services and the system <see cref="TimeProvider"/>.
        /// Call from <c>Program.cs</c>: <c>builder.Services.AddRegisteredServices();</c>
        /// </summary>
        /// <param name="services">The service collection to add the registrations to.</param>
        /// <returns>The same service collection, to allow chaining.</returns>
        public static IServiceCollection AddRegisteredServices(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICalculateService, CalculateService>();

            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IReferralService, ReferralService>();
            services.AddScoped<IPatientService, PatientService>();
            return services;
        }
    }
}
