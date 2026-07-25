using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hackathon.Infrastructure.Persistence.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly HackathonDbContext _context;

    public CampaignRepository(HackathonDbContext context)
    {
        _context = context;
    }

    public async Task<Campaign?> GetByIdAsync(Guid id)
    {
        return await _context.Campaigns.FindAsync(id);
    }

    public async Task<IEnumerable<Campaign>> GetActiveAsync()
    {
        return await _context.Campaigns
            .AsNoTracking()
            .Where(x =>
                x.Status == CampaignStatus.Ativa &&
                x.EndDate > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task AddAsync(Campaign campaign)
    {
        await _context.Campaigns.AddAsync(campaign);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Campaign campaign)
    {
        _context.Campaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }
}