namespace Aurora.Flowboard.Application.Flows.Create;

internal sealed class CreateFlowHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<CreateFlowCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateFlowCommand command,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
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

        Result<Flow> result = Flow.Create(
            command.Name,
            command.Description,
            project,
            false,
            createdBy,
            dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail<Guid>(result.Error);
        }

        Flow flow = result.Value;

        dbContext.Flows.Add(flow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return flow.Id;
    }
}
