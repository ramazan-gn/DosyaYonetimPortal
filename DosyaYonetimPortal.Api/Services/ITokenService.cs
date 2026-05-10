using DosyaYonetimPortal.Api.Entities;

namespace DosyaYonetimPortal.Api.Services;

public interface ITokenService
{
    Task<string> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
