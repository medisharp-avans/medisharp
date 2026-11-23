using Server.Services.InterFaces;

namespace Server.Services;

/// <summary>Registers the scoped services used by the referral feature.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddRegisteredServices(this IServiceCollection services)
    {
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<ReferralService>();
        services.AddScoped<ReferralUploadService>();
        services.AddScoped<ITreatmentCatalogService, TreatmentCatalogService>();
        return services;
    }
}
