using System.Security.Cryptography;
using System.Text;

namespace CentralHealth.Application.Common;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(password)));
    }
}
