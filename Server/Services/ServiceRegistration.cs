using Microsoft.Extensions.DependencyInjection;
using Server.Services.InterFaces;

namespace Server.Services
{
    /// <summary>
    /// Provides extension methods for registering application service dependencies in the dependency injection container.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all application services and the system <see cref="TimeProvider"/>.
        /// Call this from <c>Program.cs</c> using: <c>builder.Services.AddRegisteredServices();</c>
        /// </summary>
        /// <param name="services">The service collection to add the service registrations to.</param>
        /// <returns>The same service collection instance, enabling method chaining.</returns>
        public static IServiceCollection AddRegisteredServices(this IServiceCollection services)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICalculateService, CalculateService>();

            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IReferralService, ReferralService>();
            services.AddScoped<IPlanningService, PlanningService>();
            return services;
        }
    }
}