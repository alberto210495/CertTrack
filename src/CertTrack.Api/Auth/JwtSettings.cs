using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CertTrack.Api.Auth;

public class JwtSettings
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required TimeSpan Lifetime { get; init; }
    public required SymmetricSecurityKey SigningKey { get; init; }

    // True when no key was configured and a random one was made for this run.
    public bool UsesTemporaryKey { get; init; }

    public static JwtSettings Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Jwt");
        var key = section["Key"];

        byte[] keyBytes;
        if (!string.IsNullOrEmpty(key))
        {
            keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
        }
        else if (environment.IsDevelopment())
        {
            keyBytes = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            throw new InvalidOperationException(
                "Jwt:Key is not set. Set it with the Jwt__Key environment variable. It is a secret, so keep it out of appsettings.json.");
        }

        return new JwtSettings
        {
            Issuer = section["Issuer"] ?? "CertTrack",
            Audience = section["Audience"] ?? "CertTrack",
            Lifetime = TimeSpan.FromMinutes(section.GetValue("LifetimeMinutes", 120)),
            SigningKey = new SymmetricSecurityKey(keyBytes),
            UsesTemporaryKey = string.IsNullOrEmpty(key)
        };
    }
}
