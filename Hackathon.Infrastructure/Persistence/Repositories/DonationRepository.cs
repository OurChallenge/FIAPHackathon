using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Domain.Entities;

namespace Hackathon.Infrastructure.Persistence.Repositories;

public class DonationRepository : IDonationRepository
{
    private readonly HackathonDbContext _context;

    public DonationRepository(HackathonDbContext context)
    {
        _context = context;
    }

    public async Task<Donation?> GetByIdAsync(Guid id)
    {
        return await _context.Donations.FindAsync(id);
    }

    public async Task AddAsync(Donation donation)
    {
        await _context.Donations.AddAsync(donation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Donation donation)
    {
        _context.Donations.Update(donation);
        await _context.SaveChangesAsync();
    }
}