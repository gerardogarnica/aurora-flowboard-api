using Aurora.Flowboard.Application.Abstractions.Time;
using Aurora.Flowboard.Domain.Shared;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Infrastructure.Bootstrap;
using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aurora.Flowboard.Api.Extensions;

internal static class SeedingServiceExtensions
{
    internal static async Task SeedAdministratorAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        await using var dbContext = services.GetRequiredService<ApplicationDbContext>();
        IPasswordHasher passwordHasher = services.GetRequiredService<IPasswordHasher>();
        IDateTimeProvider dateTimeProvider = services.GetRequiredService<IDateTimeProvider>();
        BootstrapOptions options = services.GetRequiredService<IOptions<BootstrapOptions>>().Value;
        ILogger logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Aurora.Flowboard.Api.Bootstrap.AdministratorSeeder");

        bool administratorExists = await dbContext.Users
            .AnyAsync(u => u.Roles.Any(r => r.Name == Role.Administrator.Name));

        if (administratorExists)
        {
            return;
        }

        Result<Email> emailResult = Email.Create(options.AdminEmail);
        EnsureSuccess(emailResult);

        string passwordHash = passwordHasher.HashPassword(options.AdminPassword);

        Result<Password> passwordResult = Password.Create(passwordHash);
        EnsureSuccess(passwordResult);

        Result<User> userResult = User.Create(
            options.AdminFirstName,
            options.AdminLastName,
            emailResult.Value,
            passwordResult.Value,
            dateTimeProvider.UtcNow);

        EnsureSuccess(userResult);

        User administrator = userResult.Value;

        Result assignRoleResult = administrator.AssignRole(Role.Administrator);
        EnsureSuccess(assignRoleResult);

        dbContext.Users.Add(administrator);

        await dbContext.SaveChangesAsync();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded default Administrator user with email {AdminEmail}.", options.AdminEmail);
        }
    }

    private static void EnsureSuccess(Result result)
    {
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"Failed to seed default Administrator: {result.Error.Code} - {result.Error.Message}");
        }
    }
}
