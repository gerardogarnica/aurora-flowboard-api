namespace Aurora.Flowboard.Application.Users.GetUserById;

public sealed record UserProfileResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
