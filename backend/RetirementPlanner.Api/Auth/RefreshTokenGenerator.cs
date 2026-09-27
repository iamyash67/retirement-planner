using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RetirementPlanner.Auth
{
    public static class RefreshTokenGenerator
    {
        /// <summary>A new random refresh token (256 bits, base64url) and the hash that is stored for it.</summary>
        public static (string Token, string Hash) Create()
        {
            var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
            return (token, Hash(token));
        }

        /// <summary>
        /// SHA-256 as lowercase hex. The token is random and high-entropy, so a fast hash is enough;
        /// a password hash's work factor would only slow down every refresh.
        /// </summary>
        public static string Hash(string token) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
