using DosyaYonetimPortal.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortal.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StoredFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalFileName).HasMaxLength(512);
            e.Property(x => x.RelativePath).HasMaxLength(1024);
            e.Property(x => x.ContentType).HasMaxLength(256);
            e.Property(x => x.OwnerUserId).HasMaxLength(450);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.OwnerUserId);
            e.HasIndex(x => x.UploadedAtUtc);
        });
    }
}
