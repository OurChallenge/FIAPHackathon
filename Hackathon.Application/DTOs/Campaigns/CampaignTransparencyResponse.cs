namespace Hackathon.Application.DTOs.Campaigns;

public class CampaignTransparencyResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal FinancialGoal { get; set; }
    public decimal TotalRaised { get; set; }
}