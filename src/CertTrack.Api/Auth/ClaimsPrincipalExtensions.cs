using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CertTrack.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    // The id of the logged-in user, read from the "sub" claim of their token.
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The token has no 'sub' claim.");

        return int.Parse(sub);
    }
}
