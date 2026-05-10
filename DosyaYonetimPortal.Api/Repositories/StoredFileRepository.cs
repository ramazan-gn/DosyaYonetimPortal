using DosyaYonetimPortal.Api.Data;
using DosyaYonetimPortal.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortal.Api.Repositories;

public class StoredFileRepository : IStoredFileRepository
{
    private readonly ApplicationDbContext _db;

    public StoredFileRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        _db.StoredFiles.Remove(file);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoredFile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.StoredFiles
            .AsNoTracking()
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.StoredFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoredFile>> GetByOwnerAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.StoredFiles
            .AsNoTracking()
            .Where(f => f.OwnerUserId == userId)
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.StoredFiles.CountAsync(cancellationToken).ConfigureAwait(false);
    }
}
