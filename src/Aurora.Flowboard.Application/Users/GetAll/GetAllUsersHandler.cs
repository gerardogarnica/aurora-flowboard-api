namespace Aurora.Flowboard.Application.Users.GetAll;

internal sealed class GetAllUsersHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetAllUsersQuery, IReadOnlyCollection<UserSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<UserSummaryResponse>>> Handle(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        List<User> users = await dbContext
            .Users
            .Include(u => u.Roles)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.Initials,
                u.Email.Value,
                u.IsActive,
                u.Roles.First().Name,
                u.CreatedOnUtc,
                u.UpdatedOnUtc))
            .ToList();
    }
}
