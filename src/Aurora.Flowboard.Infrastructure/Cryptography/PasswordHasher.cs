using System.Security.Cryptography;

namespace Aurora.Flowboard.Infrastructure.Cryptography;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 310_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const byte FormatVersion = 0x01;
    private const int TotalSize = 1 + SaltSize + HashSize;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        byte[] buffer = new byte[TotalSize];
        buffer[0] = FormatVersion;
        Buffer.BlockCopy(salt, 0, buffer, 1, SaltSize);
        Buffer.BlockCopy(hash, 0, buffer, 1 + SaltSize, HashSize);

        return Convert.ToBase64String(buffer);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        byte[] buffer;
        try
        {
            buffer = Convert.FromBase64String(hashedPassword);
        }
        catch (FormatException)
        {
            return false;
        }

        if (buffer.Length != TotalSize || buffer[0] != FormatVersion)
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        byte[] storedHash = new byte[HashSize];

        Buffer.BlockCopy(buffer, 1, salt, 0, SaltSize);
        Buffer.BlockCopy(buffer, 1 + SaltSize, storedHash, 0, HashSize);

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }
}
