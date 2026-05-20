namespace Aurora.Flowboard.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserProfileResponse>;
