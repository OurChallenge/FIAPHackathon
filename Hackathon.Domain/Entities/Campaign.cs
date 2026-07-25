using Hackathon.Domain.Enums;

namespace Hackathon.Domain.Entities;

public class Campaign
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal FinancialGoal { get; private set; }
    public decimal TotalRaised { get; private set; }
    public CampaignStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Campaign()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    public Campaign(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        decimal financialGoal)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        if (endDate < DateTime.UtcNow)
            throw new ArgumentException("Campaign end date cannot be in the past.");

        if (financialGoal <= 0)
            throw new ArgumentException("Financial goal must be greater than zero.");

        if (endDate <= startDate)
            throw new ArgumentException("Campaign end date must be after start date.");

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        FinancialGoal = financialGoal;
        TotalRaised = 0;
        Status = CampaignStatus.Ativa;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddDonation(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Donation amount must be greater than zero.");

        if (Status != CampaignStatus.Ativa)
            throw new InvalidOperationException("Only active campaigns can receive donations.");

        TotalRaised += amount;
    }

    public void UpdateStatus(CampaignStatus status)
    {
        Status = status;
    }

    public void Update(
    string title,
    string description,
    DateTime startDate,
    DateTime endDate,
    decimal financialGoal,
    CampaignStatus status)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        if (endDate < DateTime.UtcNow)
            throw new ArgumentException("Campaign end date cannot be in the past.");

        if (endDate <= startDate)
            throw new ArgumentException("Campaign end date must be after start date.");

        if (financialGoal <= 0)
            throw new ArgumentException("Financial goal must be greater than zero.");

        Title = title.Trim();
        Description = description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        FinancialGoal = financialGoal;
        Status = status;
    }
}