using DosyaYonetimPortal.Api.DTOs;
using DosyaYonetimPortal.Api.Entities;
using DosyaYonetimPortal.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IStoredFileRepository _files;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(IStoredFileRepository files, UserManager<ApplicationUser> userManager)
    {
        _files = files;
        _userManager = userManager;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminStatsDto>> Stats(CancellationToken cancellationToken)
    {
        var allFiles = await _files.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var totalBytes = allFiles.Sum(f => f.SizeBytes);
        var userCount = await _userManager.Users.CountAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new AdminStatsDto
        {
            TotalFiles = allFiles.Count,
            TotalBytes = totalBytes,
            TotalUsers = userCount,
        });
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> Users(CancellationToken cancellationToken)
    {
        var result = new List<UserSummaryDto>();
        foreach (var u in await _userManager.Users.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            var roles = await _userManager.GetRolesAsync(u).ConfigureAwait(false);
            result.Add(new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                FullName = u.FullName,
                Roles = roles.ToList(),
            });
        }

        return Ok(result.OrderBy(x => x.Email));
    }

    [HttpGet("files")]
    [ProducesResponseType(typeof(IReadOnlyList<StoredFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoredFileDto>>> AllFiles(CancellationToken cancellationToken)
    {
        var list = await _files.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var dtos = new List<StoredFileDto>();
        foreach (var f in list)
        {
            var owner = await _userManager.FindByIdAsync(f.OwnerUserId).ConfigureAwait(false);
            dtos.Add(new StoredFileDto
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                UploadedAtUtc = f.UploadedAtUtc,
                Description = f.Description,
                OwnerUserId = f.OwnerUserId,
                OwnerEmail = owner?.Email ?? owner?.UserName,
            });
        }

        return Ok(dtos);
    }

    [HttpDelete("files/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(Guid id, [FromServices] IWebHostEnvironment env, CancellationToken cancellationToken)
    {
        var entity = await _files.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return NotFound();

        var root = Path.Combine(env.ContentRootPath, "FileStorage");
        var full = Path.Combine(root, entity.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);

        await _files.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
