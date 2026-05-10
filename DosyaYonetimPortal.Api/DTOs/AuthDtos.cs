using System.ComponentModel.DataAnnotations;

namespace DosyaYonetimPortal.Api.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Email { get; set; } = string.Empty;
    /// <summary>Kullanıcı kimliği (istemci tarafında dosya sahibi kontrolü için).</summary>
    public string UserId { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
