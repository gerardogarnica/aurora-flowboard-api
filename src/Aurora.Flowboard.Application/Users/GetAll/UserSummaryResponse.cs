namespace Aurora.Flowboard.Application.Users.GetAll;

public sealed record UserSummaryResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    string Initials,
    string Email,
    bool IsActive,
    string Role,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
