using Hackathon.Domain.Enums;

namespace Hackathon.Application.DTOs.Campaigns;

public class UpdateCampaignRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal FinancialGoal { get; set; }
    public CampaignStatus Status { get; set; }
}