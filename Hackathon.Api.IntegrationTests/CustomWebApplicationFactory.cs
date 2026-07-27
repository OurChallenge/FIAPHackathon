using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hackathon.Api.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] =
                    "this-is-a-test-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "FIAPHackathon.Api",
                ["Jwt:Audience"] = "FIAPHackathon.Client",
                ["Jwt:ExpirationMinutes"] = "30",

                ["ConnectionStrings:DefaultConnection"] =
                    "Server=localhost,1433;Database=FIAPHackathon;User Id=sa;Password=Test123!;TrustServerCertificate=True",

                ["RabbitMq:HostName"] = "localhost",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:UserName"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:DonationQueueName"] = "donation-received"
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthHandler.SchemeName;

                    options.DefaultForbidScheme =
                        TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}