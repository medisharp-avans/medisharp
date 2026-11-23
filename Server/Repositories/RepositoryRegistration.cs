using Server.Repositories.InterFaces;

namespace Server.Repositories;

/// <summary>Registers the repositories used by the referral feature.</summary>
public static class RepositoryRegistration
{
    public static IServiceCollection AddRegisteredRepositories(this IServiceCollection repositories)
    {
        repositories.AddScoped<IReferralRepository, ReferralRepository>();
        repositories.AddScoped<ITreatmentCatalogRepository, TreatmentCatalogRepository>();
        return repositories;
    }
}
