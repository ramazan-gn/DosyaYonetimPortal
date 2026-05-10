using System.Security.Claims;
using DosyaYonetimPortal.Api.DTOs;
using DosyaYonetimPortal.Api.Entities;
using DosyaYonetimPortal.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IStoredFileRepository _files;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _users;

    public FilesController(
        IStoredFileRepository files,
        IWebHostEnvironment env,
        UserManager<ApplicationUser> users)
    {
        _files = files;
        _env = env;
        _users = users;
    }

    private static string StorageRoot(IWebHostEnvironment env) =>
        Path.Combine(env.ContentRootPath, "FileStorage");

    /// <summary>Tüm dosyalar (paylaşımlı liste — giriş gerekmez).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<StoredFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StoredFileDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _files.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var dtos = new List<StoredFileDto>();
        foreach (var f in list)
        {
            var owner = await _users.FindByIdAsync(f.OwnerUserId).ConfigureAwait(false);
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

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(52_428_800)]
    [ProducesResponseType(typeof(StoredFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StoredFileDto>> Upload(IFormFile file, [FromForm] string? description, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Dosya seçilmedi." });

        var id = Guid.NewGuid();
        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "dosya";

        var relativeFolder = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        var storedFileName = $"{id:N}_{safeName}";
        var relativePath = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/');

        var root = StorageRoot(_env);
        var dir = Path.Combine(root, relativeFolder);
        Directory.CreateDirectory(dir);

        var physicalPath = Path.Combine(dir, storedFileName);
        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        var entity = new StoredFile
        {
            Id = id,
            OwnerUserId = userId,
            OriginalFileName = safeName,
            RelativePath = relativePath,
            ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            UploadedAtUtc = DateTime.UtcNow,
            Description = description,
        };

        await _files.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        var me = await _users.FindByIdAsync(userId).ConfigureAwait(false);
        return Ok(new StoredFileDto
        {
            Id = entity.Id,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            UploadedAtUtc = entity.UploadedAtUtc,
            Description = entity.Description,
            OwnerUserId = entity.OwnerUserId,
            OwnerEmail = me?.Email ?? me?.UserName,
        });
    }

    [HttpGet("{id:guid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _files.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return NotFound();

        var full = Path.Combine(StorageRoot(_env), entity.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(full))
            return NotFound(new { message = "Dosya sunucuda bulunamadı." });

        var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, entity.ContentType, entity.OriginalFileName);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var entity = await _files.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return NotFound();

        if (!isAdmin && entity.OwnerUserId != userId)
            return Forbid();

        var full = Path.Combine(StorageRoot(_env), entity.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);

        await _files.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
