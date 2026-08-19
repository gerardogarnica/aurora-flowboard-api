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
                ("Code Review", FlowStateCategory.Active, "navy"),
                ("In Testing", FlowStateCategory.Active, "amber"),
                ("Done", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Client] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("In Progress", FlowStateCategory.Active, "blue"),
                ("Code Review", FlowStateCategory.Active, "navy"),
                ("In Testing", FlowStateCategory.Active, "amber"),
                ("Waiting for Client", FlowStateCategory.Active, "lime"),
                ("Done", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Research] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("Investigating", FlowStateCategory.Active, "blue"),
                ("Concluded", FlowStateCategory.Completed, "green"),
                ("Cancelled", FlowStateCategory.Cancelled, "red"),
            ],
            [ProjectKind.Internal] =
            [
                ("Backlog", FlowStateCategory.Active, "slate"),
                ("In Progress", FlowStateCategory.Active, "blue"),
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

    private static void EnsureSuccess(Result result)
    {
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException(
                $"Failed to seed default Administrator: {result.Error.Code} - {result.Error.Message}");
        }
    }
}
