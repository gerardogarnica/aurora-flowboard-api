using Aurora.Flowboard.Application.Abstractions.Time;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Shared;
using Aurora.Flowboard.Domain.TemplateFlows;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Infrastructure.Bootstrap;
using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aurora.Flowboard.Api.Extensions;

internal static class SeedingServiceExtensions
{
    private static readonly IReadOnlyDictionary<ProjectKind, (string Name, FlowStateCategory Category, string Color)[]> DefaultTemplateStates =
        new Dictionary<ProjectKind, (string Name, FlowStateCategory Category, string Color)[]>
        {
            [ProjectKind.Product] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("In Progress", FlowStateCategory.Active, "blue"),
                ("Code Review", FlowStateCategory.Active, "fuchsia"),
                ("In Testing", FlowStateCategory.Active, "amber"),
                ("Ready for Release", FlowStateCategory.Active, "teal"),
                ("Released", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Client] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("In Progress", FlowStateCategory.Active, "blue"),
                ("Code Review", FlowStateCategory.Active, "fuchsia"),
                ("In Testing", FlowStateCategory.Active, "amber"),
                ("Waiting for Client", FlowStateCategory.Active, "navy"),
                ("Ready for Delivery", FlowStateCategory.Active, "teal"),
                ("Delivered", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Research] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("Investigating", FlowStateCategory.Active, "blue"),
                ("Reporting", FlowStateCategory.Active, "indigo"),
                ("Concluded", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Internal] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("In Progress", FlowStateCategory.Active, "blue"),
                ("Verifying", FlowStateCategory.Active, "orange"),
                ("Done", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
        };

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

        // Inactive users cannot log in, so only an active Administrator counts.
        bool activeAdministratorExists = await dbContext.Users
            .AnyAsync(u => u.IsActive && u.Roles.Any(r => r.Name == Role.Administrator.Name));

        if (activeAdministratorExists)
        {
            return;
        }

        Result<Email> emailResult = Email.Create(options.AdminEmail);
        EnsureSuccess(emailResult);

        string emailValue = emailResult.Value.Value;

        // The bootstrap user may still exist (e.g. demoted or deactivated): restore it instead of
        // inserting a duplicate, which the unique email index would reject and abort startup.
        User? existingUser = await dbContext.Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Email.Value == emailValue);

        if (existingUser is not null)
        {
            RestoreAdministrator(existingUser, dateTimeProvider.UtcNow);

            await dbContext.SaveChangesAsync();

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "No active Administrator found. Restored Administrator access for existing user {UserId}.",
                    existingUser.Id);
            }

            return;
        }

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

    internal static async Task SeedTemplateFlowsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        await using var dbContext = services.GetRequiredService<ApplicationDbContext>();
        IDateTimeProvider dateTimeProvider = services.GetRequiredService<IDateTimeProvider>();
        ILogger logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Aurora.Flowboard.Api.Bootstrap.TemplateFlowSeeder");

        Guid administratorId = await dbContext.Users
            .Where(u => u.Roles.Any(r => r.Name == Role.Administrator.Name))
            .Select(u => u.Id)
            .FirstAsync();

        var seededKinds = new List<ProjectKind>();

        foreach (ProjectKind kind in DefaultTemplateStates.Keys)
        {
            bool templateExists = await dbContext.TemplateFlows.AnyAsync(t => t.Kind == kind);

            if (templateExists)
            {
                continue;
            }

            Result<TemplateFlow> templateResult = TemplateFlow.Create(kind, administratorId, dateTimeProvider.UtcNow);
            EnsureSuccess(templateResult);

            TemplateFlow template = templateResult.Value;

            foreach ((string name, FlowStateCategory category, string color) in DefaultTemplateStates[kind])
            {
                Result<Color> colorResult = Color.Create(color);
                EnsureSuccess(colorResult);

                Result addStateResult = template.AddState(name, category, colorResult.Value);
                EnsureSuccess(addStateResult);
            }

            dbContext.TemplateFlows.Add(template);
            seededKinds.Add(kind);
        }

        if (seededKinds.Count == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded default TemplateFlow for kinds {ProjectKinds}.", string.Join(", ", seededKinds));
        }
    }

    private static void RestoreAdministrator(User user, DateTime utcNow)
    {
        if (!user.IsActive)
        {
            EnsureSuccess(user.Reactivate(utcNow));
        }

        if (!user.IsAdministrator)
        {
            // A promotion never trips the last-Administrator guard, so the flag is irrelevant here.
            EnsureSuccess(user.ChangeRole(Role.Administrator, hasOtherActiveAdministrator: false, utcNow));
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
