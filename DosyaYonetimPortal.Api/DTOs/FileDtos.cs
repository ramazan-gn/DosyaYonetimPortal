namespace DosyaYonetimPortal.Api.DTOs;

public class StoredFileDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? Description { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
}

public class AdminStatsDto
{
    public int TotalFiles { get; set; }
    public long TotalBytes { get; set; }
    public int TotalUsers { get; set; }
}
