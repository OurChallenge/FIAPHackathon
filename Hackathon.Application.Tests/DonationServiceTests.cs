using Hackathon.Application.DTOs.Donations;
using Hackathon.Application.Events;
using Hackathon.Application.Interfaces.Messaging;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Application.Services;
using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;
using Moq;

namespace Hackathon.Application.Tests;

public class DonationServiceTests
{
    private readonly Mock<ICampaignRepository> _campaignRepositoryMock;
    private readonly Mock<IDonationRepository> _donationRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DonationService _donationService;

    public DonationServiceTests()
    {
        _campaignRepositoryMock = new Mock<ICampaignRepository>();
        _donationRepositoryMock = new Mock<IDonationRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _donationService = new DonationService(
            _campaignRepositoryMock.Object,
            _donationRepositoryMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Create_WithNonExistingCampaign_ShouldThrowKeyNotFoundException()
    {
        var request = new CreateDonationRequest
        {
            CampaignId = Guid.NewGuid(),
            Amount = 100m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CampaignId))
            .ReturnsAsync((Campaign?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _donationService.CreateAsync(Guid.NewGuid(), request));

        _donationRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Donation>()),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()),
            Times.Never);
    }

    [Theory]
    [InlineData(CampaignStatus.Cancelada)]
    [InlineData(CampaignStatus.Concluida)]
    public async Task Create_WithInactiveCampaign_ShouldThrowInvalidOperationException(
        CampaignStatus status)
    {
        var campaign = CreateValidCampaign();
        campaign.UpdateStatus(status);

        var request = new CreateDonationRequest
        {
            CampaignId = campaign.Id,
            Amount = 100m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(campaign.Id))
            .ReturnsAsync(campaign);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _donationService.CreateAsync(Guid.NewGuid(), request));

        _donationRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Donation>()),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WithEndedCampaign_ShouldThrowInvalidOperationException()
    {
        var campaign = CreateValidCampaign();

        // Como a entidade não deixa criar campanha já encerrada,
        // vamos usar Update para colocar uma data muito próxima e
        // testar o comportamento do service de forma controlada.
        // Se isso não for possível no domínio atual, ajustamos este teste.
        campaign.Update(
            campaign.Title,
            campaign.Description,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddMilliseconds(50),
            campaign.FinancialGoal,
            CampaignStatus.Ativa);

        await Task.Delay(100);

        var request = new CreateDonationRequest
        {
            CampaignId = campaign.Id,
            Amount = 100m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(campaign.Id))
            .ReturnsAsync(campaign);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _donationService.CreateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task Create_WithValidDonation_ShouldPersistDonation()
    {
        var campaign = CreateValidCampaign();
        var donorId = Guid.NewGuid();

        var request = new CreateDonationRequest
        {
            CampaignId = campaign.Id,
            Amount = 250m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(campaign.Id))
            .ReturnsAsync(campaign);

        Donation? savedDonation = null;

        _donationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Donation>()))
            .Callback<Donation>(donation => savedDonation = donation)
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()))
            .Returns(Task.CompletedTask);

        var donationId = await _donationService.CreateAsync(
            donorId,
            request);

        Assert.NotEqual(Guid.Empty, donationId);
        Assert.NotNull(savedDonation);

        Assert.Equal(campaign.Id, savedDonation!.CampaignId);
        Assert.Equal(donorId, savedDonation.DonorId);
        Assert.Equal(250m, savedDonation.Amount);
        Assert.Equal(DonationStatus.Pendente, savedDonation.Status);

        _donationRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Donation>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WithValidDonation_ShouldPublishDonationReceivedEvent()
    {
        var campaign = CreateValidCampaign();
        var donorId = Guid.NewGuid();

        var request = new CreateDonationRequest
        {
            CampaignId = campaign.Id,
            Amount = 350m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(campaign.Id))
            .ReturnsAsync(campaign);

        _donationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Donation>()))
            .Returns(Task.CompletedTask);

        DonationReceivedEvent? publishedEvent = null;

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()))
            .Callback<DonationReceivedEvent>(
                donationEvent => publishedEvent = donationEvent)
            .Returns(Task.CompletedTask);

        await _donationService.CreateAsync(donorId, request);

        Assert.NotNull(publishedEvent);
        Assert.Equal(campaign.Id, publishedEvent!.CampaignId);
        Assert.Equal(donorId, publishedEvent.DonorId);
        Assert.Equal(350m, publishedEvent.Amount);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_WithValidDonation_ShouldNotUpdateCampaignDirectly()
    {
        var campaign = CreateValidCampaign();

        var request = new CreateDonationRequest
        {
            CampaignId = campaign.Id,
            Amount = 500m
        };

        _campaignRepositoryMock
            .Setup(x => x.GetByIdAsync(campaign.Id))
            .ReturnsAsync(campaign);

        _donationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Donation>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<DonationReceivedEvent>()))
            .Returns(Task.CompletedTask);

        await _donationService.CreateAsync(Guid.NewGuid(), request);

        Assert.Equal(0m, campaign.TotalRaised);

        _campaignRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Campaign>()),
            Times.Never);
    }

    private static Campaign CreateValidCampaign()
    {
        return new Campaign(
            "Campanha Solidária",
            "Campanha criada para testes.",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            10000m);
    }
}