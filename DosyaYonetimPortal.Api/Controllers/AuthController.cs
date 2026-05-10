using System.Security.Claims;
using DosyaYonetimPortal.Api.DTOs;
using DosyaYonetimPortal.Api.Entities;
using DosyaYonetimPortal.Api.Options;
using DosyaYonetimPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DosyaYonetimPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwt;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<JwtSettings> jwt)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _jwt = jwt.Value;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
            return BadRequest(new { message = "Bu e-posta ile kayıtlı kullanıcı zaten var." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
        };

        var create = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!create.Succeeded)
            return BadRequest(new { message = string.Join(" ", create.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, "User").ConfigureAwait(false);

        return Ok(await BuildAuthResponseAsync(user, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
            return Unauthorized(new { message = "E-posta veya şifre hatalı." });

        var valid = await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!valid)
            return Unauthorized(new { message = "E-posta veya şifre hatalı." });

        return Ok(await BuildAuthResponseAsync(user, cancellationToken).ConfigureAwait(false));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Me(CancellationToken cancellationToken)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null)
            return Unauthorized();

        return Ok(await BuildAuthResponseAsync(user, cancellationToken).ConfigureAwait(false));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await _tokenService.CreateAccessTokenAsync(user, cancellationToken).ConfigureAwait(false);
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            Email = user.Email ?? user.UserName ?? user.Id,
            UserId = user.Id,
            Roles = roles,
        };
    }
}
