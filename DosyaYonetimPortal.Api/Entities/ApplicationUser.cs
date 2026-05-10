using Microsoft.AspNetCore.Identity;

namespace DosyaYonetimPortal.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
