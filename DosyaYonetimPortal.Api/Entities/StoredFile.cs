namespace DosyaYonetimPortal.Api.Entities;

public class StoredFile
{
    public Guid Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    /// <summary>
    /// Relative path under the API FileStorage root (e.g. ab/cd/guid_filename.pdf).
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? Description { get; set; }
}
