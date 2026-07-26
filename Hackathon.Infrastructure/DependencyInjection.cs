using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Application.Interfaces.Security;
using Hackathon.Infrastructure.Persistence;
using Hackathon.Infrastructure.Persistence.Repositories;
using Hackathon.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hackathon.Application.Interfaces.Messaging;
using Hackathon.Infrastructure.Messaging;

namespace Hackathon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<HackathonDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RabbitMqSettings>(
        configuration.GetSection(RabbitMqSettings.SectionName));

        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IDonationRepository, DonationRepository>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}