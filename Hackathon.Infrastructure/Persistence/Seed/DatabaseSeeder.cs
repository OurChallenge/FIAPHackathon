using Hackathon.Domain.Entities;
using Hackathon.Domain.Enums;
using Hackathon.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hackathon.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(HackathonDbContext context)
    {
        const string managerEmail = "gestor@fiaphackathon.com";

        var managerExists = await context.Users
            .AnyAsync(x => x.Email == managerEmail);

        if (managerExists)
            return;

        var passwordHasher = new BCryptPasswordHasher();

        var manager = new User(
            "Gestor ONG",
            managerEmail,
            "52998224725",
            passwordHasher.Hash("Gestor@123"),
            UserRole.GestorONG);

        await context.Users.AddAsync(manager);
        await context.SaveChangesAsync();
    }
}