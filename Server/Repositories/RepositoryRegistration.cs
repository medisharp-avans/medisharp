using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    /// <summary>Dependency injection registrations for the program repositories.</summary>
    public static class RepositoryRegistration
    {
        /// <summary>
        /// Registers all program repositories and the system />.
        /// Call from <c>Program.cs</c>: <c>builder.Services.AddRegisteredRepositories();</c>
        /// </summary>
        /// <param name="repositories">The service collection to add the registrations to.</param>
        /// <returns>The same repositories collection, to allow chaining.</returns>
        public static IServiceCollection AddRegisteredRepositories(this IServiceCollection repositories)
        {
            repositories.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            repositories.AddScoped<ITreatmentCodeRepository, TreatmentCodeRepository>();
            repositories.AddScoped<IAppointmentRepository,AppointmentRepository>();
            repositories.AddScoped<IReferralRepository,ReferralRepository>();
            repositories.AddScoped<IPatientRepository,PatientRepository>();
            return repositories;
        }
    }
}