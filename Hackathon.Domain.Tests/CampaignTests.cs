using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;

namespace Hackathon.Domain.Tests;

public class CampaignTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateActiveCampaign()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(30);
        const decimal financialGoal = 10000m;

        // Act
        var campaign = new Campaign(
            "Campanha de Inverno",
            "Arrecadação para famílias em situação de vulnerabilidade.",
            startDate,
            endDate,
            financialGoal);

        // Assert
        Assert.NotEqual(Guid.Empty, campaign.Id);
        Assert.Equal("Campanha de Inverno", campaign.Title);
        Assert.Equal(financialGoal, campaign.FinancialGoal);
        Assert.Equal(0m, campaign.TotalRaised);
        Assert.Equal(CampaignStatus.Ativa, campaign.Status);
    }

    [Fact]
    public void Constructor_WithPastEndDate_ShouldThrowArgumentException()
    {
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Campaign(
                "Campanha inválida",
                "Descrição",
                startDate,
                endDate,
                1000m));

        Assert.Equal(
            "Campaign end date cannot be in the past.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WithZeroFinancialGoal_ShouldThrowArgumentException()
    {
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(10);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Campaign(
                "Campanha",
                "Descrição",
                startDate,
                endDate,
                0m));

        Assert.Equal(
            "Financial goal must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WithNegativeFinancialGoal_ShouldThrowArgumentException()
    {
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(10);

        Assert.Throws<ArgumentException>(() =>
            new Campaign(
                "Campanha",
                "Descrição",
                startDate,
                endDate,
                -100m));
    }

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        var startDate = DateTime.UtcNow.AddDays(10);
        var endDate = DateTime.UtcNow.AddDays(5);

        Assert.Throws<ArgumentException>(() =>
            new Campaign(
                "Campanha",
                "Descrição",
                startDate,
                endDate,
                1000m));
    }

    [Fact]
    public void AddDonation_WithValidAmount_ShouldIncreaseTotalRaised()
    {
        var campaign = new Campaign(
            "Campanha",
            "Descrição",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            1000m);

        campaign.AddDonation(250m);

        Assert.Equal(250m, campaign.TotalRaised);
    }

    [Fact]
    public void AddDonation_WithZeroAmount_ShouldThrowArgumentException()
    {
        var campaign = new Campaign(
            "Campanha",
            "Descrição",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            1000m);

        Assert.Throws<ArgumentException>(() =>
            campaign.AddDonation(0m));
    }

    [Fact]
    public void AddDonation_WhenCampaignIsCancelled_ShouldThrowInvalidOperationException()
    {
        var campaign = new Campaign(
            "Campanha",
            "Descrição",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            1000m);

        campaign.UpdateStatus(CampaignStatus.Cancelada);

        Assert.Throws<InvalidOperationException>(() =>
            campaign.AddDonation(100m));
    }
}