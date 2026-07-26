using Hackathon.Domain.Entities;

namespace Hackathon.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> CpfExistsAsync(string cpf);
    Task AddAsync(User user);
}