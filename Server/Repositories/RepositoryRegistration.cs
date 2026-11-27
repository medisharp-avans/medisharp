using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    /// <summary>Dependency injection registrations for the program repositories.</summary>
    public static class RepositoryRegistration
    {
        /// <summary>
        /// Registers the scoped repositories for the application.
        /// Call from <c>Program.cs</c>: <c>builder.Services.AddRegisteredRepositories();</c>
        /// </summary>
        /// <param name="repositories">The service collection to add the registrations to.</param>
        /// <returns>The same repositories collection, to allow chaining.</returns>
        public static IServiceCollection AddRegisteredRepositories(this IServiceCollection repositories)
        {
            repositories.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            repositories.AddScoped<IUserRepository, UserRepository>();
            repositories.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            repositories.AddScoped<IReferralIntakeRepository, ReferralIntakeRepository>();
            repositories.AddScoped<ITreatmentCatalogRepository, TreatmentCatalogRepository>();
            return repositories;
        }
    }
}
