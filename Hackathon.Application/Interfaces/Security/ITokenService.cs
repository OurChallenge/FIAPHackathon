using Hackathon.Domain.Entities;

namespace Hackathon.Application.Interfaces.Security;

public interface ITokenService
{
    string GenerateToken(User user);
}