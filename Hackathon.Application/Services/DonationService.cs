using Hackathon.Application.DTOs.Donations;
using Hackathon.Application.Events;
using Hackathon.Application.Interfaces.Messaging;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;

namespace Hackathon.Application.Services;

public class DonationService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IEventPublisher _eventPublisher;

    public DonationService(
        ICampaignRepository campaignRepository,
        IDonationRepository donationRepository,
        IEventPublisher eventPublisher)
    {
        _campaignRepository = campaignRepository;
        _donationRepository = donationRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> CreateAsync(
        Guid donorId,
        CreateDonationRequest request)
    {
        var campaign = await _campaignRepository.GetByIdAsync(
            request.CampaignId);

        if (campaign is null)
            throw new KeyNotFoundException("Campaign not found.");

        if (campaign.Status != CampaignStatus.Ativa)
            throw new InvalidOperationException(
                "Donations are only allowed for active campaigns.");

        if (campaign.EndDate <= DateTime.UtcNow)
            throw new InvalidOperationException(
                "Donations are not allowed for ended campaigns.");

        var donation = new Donation(
            request.CampaignId,
            donorId,
            request.Amount);

        await _donationRepository.AddAsync(donation);

        var donationEvent = new DonationReceivedEvent
        {
            DonationId = donation.Id,
            CampaignId = donation.CampaignId,
            DonorId = donation.DonorId,
            Amount = donation.Amount,
            CreatedAt = donation.CreatedAt
        };

        await _eventPublisher.PublishAsync(donationEvent);

        return donation.Id;
    }
}