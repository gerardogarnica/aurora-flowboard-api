namespace Aurora.Flowboard.Application.Users.GetAll;

public sealed record UserSummaryResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Initials,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
