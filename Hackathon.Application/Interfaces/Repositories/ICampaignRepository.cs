using Hackathon.Domain.Entities;

namespace Hackathon.Application.Interfaces.Repositories;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id);
    Task<IEnumerable<Campaign>> GetActiveAsync();
    Task AddAsync(Campaign campaign);
    Task UpdateAsync(Campaign campaign);
}