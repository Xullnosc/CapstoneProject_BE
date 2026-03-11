using System.Security.Cryptography;
using System.Text;

namespace Services.Helpers;

public static class RefreshTokenHelper
{
    private const int TokenByteLength = 64;

    /// <summary>
    /// Generates a cryptographically random token and its SHA256 hash.
    /// </summary>
    public static (string Token, string TokenHash) GenerateTokenAndHash()
    {
        var bytes = new byte[TokenByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        var token = Convert.ToBase64String(bytes);
        var hash = ComputeHash(token);
        return (token, hash);
    }

    public static string ComputeHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}
