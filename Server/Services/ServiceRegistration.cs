using Microsoft.AspNetCore.Identity;
using Server.Models;
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
            services.AddMemoryCache();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReferralIntakeService, ReferralIntakeService>();
            services.AddScoped<ReferralUploadService>();
            services.AddScoped<ITreatmentCatalogService, TreatmentCatalogService>();
            return services;
        }
    }
}
