using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment;
    private int _nextId;

    public InMemoryEquipmentRepository(IEnumerable<Equipment>? seedData = null)
    {
        _equipment = seedData?.ToList() ?? new List<Equipment>();
        _nextId = _equipment.Count > 0 ? _equipment.Max(e => e.Id) + 1 : 1;
    }

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task<List<Equipment>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.Where(e => ids.Contains(e.Id)).ToList());

    public Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.OrderBy(e => e.Identifier).ThenBy(e => e.Id).ToList());

    public Task<List<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.Where(e => e.IsAvailable).OrderBy(e => e.Identifier).ThenBy(e => e.Id).ToList());

    public Task<Equipment?> FindByNormalizedIdentifierAsync(string normalizedIdentifier, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.NormalizedIdentifier == normalizedIdentifier));

    public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        if (equipment.Id == 0)
        {
            typeof(Equipment).GetProperty(nameof(Equipment.Id))!.SetValue(equipment, _nextId++);
        }
        _equipment.Add(equipment);
        return Task.CompletedTask;
    }
}
