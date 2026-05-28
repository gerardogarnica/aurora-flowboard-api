namespace Aurora.Flowboard.Domain.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);

    // Performs a verification against a known dummy hash to keep response time
    // constant for unknown-user lookups (mitigates account-enumeration timing attacks).
    void VerifyDummy(string providedPassword);
}