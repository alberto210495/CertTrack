using CertTrack.Api.Auth;
using CertTrack.Api.Contracts;
using CertTrack.Api.Data;
using CertTrack.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "An account with this email already exists.");

        var user = new User { Email = email, PasswordHash = "" };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResponse(tokens.CreateToken(user), user.Email);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // The same answer for an unknown email and a wrong password, so the response
        // does not reveal which emails have an account.
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Wrong email or password.");

        return new AuthResponse(tokens.CreateToken(user), user.Email);
    }

    private static string Normalize(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
