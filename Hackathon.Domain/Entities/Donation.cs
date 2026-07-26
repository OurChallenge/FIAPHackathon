using Hackathon.Domain.Enums;

namespace Hackathon.Domain.Entities;

public class Donation
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid DonorId { get; private set; }
    public decimal Amount { get; private set; }
    public DonationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Donation()
    {
    }

    public Donation(
        Guid campaignId,
        Guid donorId,
        decimal amount)
    {
        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign id is required.");

        if (donorId == Guid.Empty)
            throw new ArgumentException("Donor id is required.");

        if (amount <= 0)
            throw new ArgumentException("Donation amount must be greater than zero.");

        Id = Guid.NewGuid();
        CampaignId = campaignId;
        DonorId = donorId;
        Amount = amount;
        Status = DonationStatus.Pendente;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        Status = DonationStatus.Processada;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = DonationStatus.Falhou;
    }
}