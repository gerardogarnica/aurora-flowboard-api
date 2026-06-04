namespace Aurora.Flowboard.Application.Users.GetUserById;

internal sealed class GetUserByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetUserByIdQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail<UserProfileResponse>(UserErrors.NotFound);
        }

        return new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Initials,
            user.Email.Value,
            user.IsActive,
            [.. user.Roles.Select(r => r.Name)],
            user.CreatedOnUtc,
            user.UpdatedOnUtc);
    }
}
