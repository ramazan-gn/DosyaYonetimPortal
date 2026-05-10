using DosyaYonetimPortal.Api.Entities;

namespace DosyaYonetimPortal.Api.Repositories;

public interface IStoredFileRepository
{
    Task<IReadOnlyList<StoredFile>> GetByOwnerAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredFile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
