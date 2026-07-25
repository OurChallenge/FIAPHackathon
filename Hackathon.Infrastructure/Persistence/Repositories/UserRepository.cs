using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hackathon.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HackathonDbContext _context;

    public UserRepository(HackathonDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .AnyAsync(x => x.Email == normalizedEmail);
    }
    public async Task<bool> CpfExistsAsync(string cpf)
    {
        return await _context.Users
            .AnyAsync(x => x.Cpf == cpf);
    }
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}