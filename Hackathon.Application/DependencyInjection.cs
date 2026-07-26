using Hackathon.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hackathon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<CampaignService>();
        services.AddScoped<DonationService>();

        return services;
    }
}