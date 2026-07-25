using Hackathon.Application.DTOs.Auth;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Application.Interfaces.Security;
using Hackathon.Application.Validators;
using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;

namespace Hackathon.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Guid> RegisterDonorAsync(RegisterDonorRequest request)
    {
        if (!CpfValidator.IsValid(request.Cpf))
            throw new ArgumentException("Invalid CPF.");

        var normalizedCpf = CpfValidator.Normalize(request.Cpf);

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException(
                "Email is already registered.");

        if (await _userRepository.CpfExistsAsync(normalizedCpf))
            throw new InvalidOperationException(
                "CPF is already registered.");

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(
            request.FullName,
            request.Email,
            normalizedCpf,
            passwordHash,
            UserRole.Doador);

        await _userRepository.AddAsync(user);

        return user.Id;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant());

        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token
        };
    }
}