using DosyaYonetimPortal.Api.Data;
using DosyaYonetimPortal.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DosyaYonetimPortal.Api.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
        }

        const string adminEmail = "admin@dosyaportal.local";
        var admin = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Sistem Yöneticisi",
            };
            var result = await userManager.CreateAsync(admin, "Admin123!").ConfigureAwait(false);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin").ConfigureAwait(false);
        }

        await SeedOrnekDosyalarAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Veritabanında hiç dosya yoksa, SeedOrnekDosyalar klasöründeki metin dosyalarını
    /// yönetici hesabına ve FileStorage altına kopyalar (demo içerik).
    /// </summary>
    private static async Task SeedOrnekDosyalarAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.StoredFiles.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var env = services.GetRequiredService<IHostEnvironment>();

        const string adminEmail = "admin@dosyaportal.local";
        var admin = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);
        if (admin is null)
            return;

        var seedDir = Path.Combine(env.ContentRootPath, "SeedOrnekDosyalar");
        if (!Directory.Exists(seedDir))
            return;

        var storageRoot = Path.Combine(env.ContentRootPath, "FileStorage");
        Directory.CreateDirectory(storageRoot);

        var files = Directory.GetFiles(seedDir);
        if (files.Length == 0)
            return;

        var now = DateTime.UtcNow;
        var monthFolder = Path.Combine(now.ToString("yyyy"), now.ToString("MM"));

        foreach (var srcPath in files)
        {
            var originalName = Path.GetFileName(srcPath);
            var id = Guid.NewGuid();
            var storedFileName = $"{id:N}_{originalName}";
            var relativePath = Path.Combine(monthFolder, storedFileName).Replace('\\', '/');

            var destDir = Path.Combine(storageRoot, monthFolder);
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, storedFileName);
            File.Copy(srcPath, destPath, overwrite: false);

            var info = new FileInfo(destPath);
            db.StoredFiles.Add(new StoredFile
            {
                Id = id,
                OwnerUserId = admin.Id,
                OriginalFileName = originalName,
                RelativePath = relativePath,
                ContentType = "text/plain",
                SizeBytes = info.Length,
                UploadedAtUtc = now,
                Description = "Örnek dosya (ilk kurulumda otomatik eklendi)",
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
