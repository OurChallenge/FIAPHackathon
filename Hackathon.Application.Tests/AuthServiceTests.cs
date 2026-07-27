using Hackathon.Application.DTOs.Auth;
using Hackathon.Application.Interfaces.Repositories;
using Hackathon.Application.Interfaces.Security;
using Hackathon.Application.Services;
using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;
using Moq;

namespace Hackathon.Application.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterDonor_WithValidData_ShouldCreateDonor()
    {
        var request = new RegisterDonorRequest
        {
            FullName = "João da Silva",
            Email = "joao@email.com",
            Cpf = "52998224725",
            Password = "Senha@123"
        };

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.CpfExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.Hash(request.Password))
            .Returns("hashed-password");

        User? savedUser = null;

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var userId = await _authService.RegisterDonorAsync(request);

        Assert.NotEqual(Guid.Empty, userId);
        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.Doador, savedUser!.Role);
        Assert.Equal("joao@email.com", savedUser.Email);
        Assert.Equal("52998224725", savedUser.Cpf);
        Assert.Equal("hashed-password", savedUser.PasswordHash);

        _passwordHasherMock.Verify(
            x => x.Hash(request.Password),
            Times.Once);

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterDonor_WithInvalidCpf_ShouldThrowArgumentException()
    {
        var request = new RegisterDonorRequest
        {
            FullName = "João da Silva",
            Email = "joao@email.com",
            Cpf = "11111111111",
            Password = "Senha@123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _authService.RegisterDonorAsync(request));
    }

    [Fact]
    public async Task RegisterDonor_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        var request = new RegisterDonorRequest
        {
            FullName = "João da Silva",
            Email = "joao@email.com",
            Cpf = "52998224725",
            Password = "Senha@123"
        };

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterDonorAsync(request));
    }

    [Fact]
    public async Task RegisterDonor_WithExistingCpf_ShouldThrowInvalidOperationException()
    {
        var request = new RegisterDonorRequest
        {
            FullName = "João da Silva",
            Email = "joao@email.com",
            Cpf = "52998224725",
            Password = "Senha@123"
        };

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.CpfExistsAsync("52998224725"))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterDonorAsync(request));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var user = new User(
            "João da Silva",
            "joao@email.com",
            "52998224725",
            "hashed-password",
            UserRole.Doador);

        var request = new LoginRequest
        {
            Email = "joao@email.com",
            Password = "Senha@123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("joao@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns("jwt-token");

        var response = await _authService.LoginAsync(request);

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal("Doador", response.Role);
        Assert.Equal("jwt-token", response.Token);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        var user = new User(
            "João da Silva",
            "joao@email.com",
            "52998224725",
            "hashed-password",
            UserRole.Doador);

        var request = new LoginRequest
        {
            Email = "joao@email.com",
            Password = "senha-errada"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("joao@email.com"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(request));
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldThrowUnauthorizedAccessException()
    {
        var request = new LoginRequest
        {
            Email = "naoexiste@email.com",
            Password = "Senha@123"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("naoexiste@email.com"))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(request));
    }
}