namespace Aurora.Flowboard.Application.Components.Create;

internal sealed class CreateComponentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateComponentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateComponentCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Components)
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail<Guid>(ProjectErrors.NotFound);
        }

        User? createdBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userContext.UserId, cancellationToken);

        if (createdBy is null)
        {
            return Result.Fail<Guid>(UserErrors.NotFound);
        }

        Result result = project.AddComponent(command.Name, createdBy, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Component component = project.Components.Last();

        await dbContext.SaveChangesAsync(cancellationToken);

        return component.Id;
    }
}
