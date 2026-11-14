using System.ComponentModel.DataAnnotations;

namespace CertTrack.Api.Contracts;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record AuthResponse(string Token, string Email);
