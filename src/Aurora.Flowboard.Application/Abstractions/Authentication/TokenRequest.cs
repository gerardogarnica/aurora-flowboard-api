namespace Aurora.Flowboard.Application.Abstractions.Authentication;

public sealed record TokenRequest(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    IEnumerable<string> Roles);
