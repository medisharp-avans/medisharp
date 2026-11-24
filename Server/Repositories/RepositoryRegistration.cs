using Microsoft.Extensions.DependencyInjection;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    /// <summary>
    /// Provides extension methods for registering repository dependencies in the dependency injection container.
    /// </summary>
    public static class RepositoryRegistration
    {
        /// <summary>
        /// Registers all application repositories and the generic repository implementation.
        /// Call this from <c>Program.cs</c> using: <c>builder.Services.AddRegisteredRepositories();</c>
        /// </summary>
        /// <param name="repositories">The service collection to add the repository registrations to.</param>
        /// <returns>The same service collection instance, enabling method chaining.</returns>
        public static IServiceCollection AddRegisteredRepositories(this IServiceCollection repositories)
        {
            repositories.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            repositories.AddScoped<ITreatmentCodeRepository, TreatmentCodeRepository>();
            repositories.AddScoped<IAppointmentRepository, AppointmentRepository>();
            repositories.AddScoped<IReferralRepository, ReferralRepository>();
            repositories.AddScoped<IPlanningRepository, PlanningRepository>();
            return repositories;
        }
    }
}