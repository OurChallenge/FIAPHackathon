using Hackathon.Domain.Entities;

namespace Hackathon.Application.Interfaces.Repositories;

public interface IDonationRepository
{
    Task<Donation?> GetByIdAsync(Guid id);
    Task AddAsync(Donation donation);
    Task UpdateAsync(Donation donation);
}