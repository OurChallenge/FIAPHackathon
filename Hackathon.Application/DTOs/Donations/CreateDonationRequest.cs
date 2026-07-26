namespace Hackathon.Application.DTOs.Donations;

public class CreateDonationRequest
{
    public Guid CampaignId { get; set; }
    public decimal Amount { get; set; }
}