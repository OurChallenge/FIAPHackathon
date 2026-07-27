using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;

namespace Hackathon.Domain.Tests;

public class DonationTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreatePendingDonation()
    {
        var campaignId = Guid.NewGuid();
        var donorId = Guid.NewGuid();

        var donation = new Donation(
            campaignId,
            donorId,
            150m);

        Assert.NotEqual(Guid.Empty, donation.Id);
        Assert.Equal(campaignId, donation.CampaignId);
        Assert.Equal(donorId, donation.DonorId);
        Assert.Equal(150m, donation.Amount);
        Assert.Equal(DonationStatus.Pendente, donation.Status);
        Assert.Null(donation.ProcessedAt);
    }

    [Fact]
    public void Constructor_WithEmptyCampaignId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Donation(
                Guid.Empty,
                Guid.NewGuid(),
                100m));
    }

    [Fact]
    public void Constructor_WithEmptyDonorId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Donation(
                Guid.NewGuid(),
                Guid.Empty,
                100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(
        decimal amount)
    {
        Assert.Throws<ArgumentException>(() =>
            new Donation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                amount));
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetStatusAndProcessedAt()
    {
        var donation = new Donation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);

        donation.MarkAsProcessed();

        Assert.Equal(DonationStatus.Processada, donation.Status);
        Assert.NotNull(donation.ProcessedAt);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailedStatus()
    {
        var donation = new Donation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m);

        donation.MarkAsFailed();

        Assert.Equal(DonationStatus.Falhou, donation.Status);
    }
}