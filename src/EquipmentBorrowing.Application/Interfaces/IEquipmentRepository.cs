using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Equipment>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<Equipment?> FindByNormalizedIdentifierAsync(string normalizedIdentifier, CancellationToken cancellationToken = default);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
}
