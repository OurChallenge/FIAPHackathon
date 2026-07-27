using System.Net;
using System.Net.Http.Json;

namespace Hackathon.Api.IntegrationTests;

public class AuthorizationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCampaign_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/campaigns",
            new
            {
                title = "Campanha Teste",
                description = "Descrição",
                startDate = DateTime.UtcNow.AddDays(1),
                endDate = DateTime.UtcNow.AddDays(30),
                financialGoal = 1000m
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDonation_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/donations",
            new
            {
                campaignId = Guid.NewGuid(),
                amount = 100m
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCampaign_WithDonorRole_ShouldReturnForbidden()
    {
        _client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Doador");

        var response = await _client.PostAsJsonAsync(
            "/api/campaigns",
            new
            {
                title = "Campanha Teste",
                description = "Descrição",
                startDate = DateTime.UtcNow.AddDays(1),
                endDate = DateTime.UtcNow.AddDays(30),
                financialGoal = 1000m
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDonation_WithManagerRole_ShouldReturnForbidden()
    {
        _client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "GestorONG");

        var response = await _client.PostAsJsonAsync(
            "/api/donations",
            new
            {
                campaignId = Guid.NewGuid(),
                amount = 100m
            });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}