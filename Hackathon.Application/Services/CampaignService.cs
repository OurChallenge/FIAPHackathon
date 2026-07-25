using Hackathon.Application.DTOs.Campaigns;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Domain.Entities;

namespace Hackathon.Application.Services;

public class CampaignService
{
    private readonly ICampaignRepository _campaignRepository;

    public CampaignService(ICampaignRepository campaignRepository)
    {
        _campaignRepository = campaignRepository;
    }

    public async Task<Guid> CreateAsync(CreateCampaignRequest request)
    {
        var campaign = new Campaign(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.FinancialGoal);

        await _campaignRepository.AddAsync(campaign);

        return campaign.Id;
    }

    public async Task UpdateAsync(
        Guid campaignId,
        UpdateCampaignRequest request)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId);

        if (campaign is null)
            throw new KeyNotFoundException("Campaign not found.");

        campaign.Update(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.FinancialGoal,
            request.Status);

        await _campaignRepository.UpdateAsync(campaign);
    }

    public async Task<IEnumerable<CampaignTransparencyResponse>>
        GetActiveCampaignsAsync()
    {
        var campaigns = await _campaignRepository.GetActiveAsync();

        return campaigns.Select(c => new CampaignTransparencyResponse
        {
            Id = c.Id,
            Title = c.Title,
            FinancialGoal = c.FinancialGoal,
            TotalRaised = c.TotalRaised
        });
    }
}