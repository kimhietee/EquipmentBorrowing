using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment;

    public InMemoryEquipmentRepository(IEnumerable<Equipment>? seedData = null)
    {
        _equipment = seedData?.ToList() ?? new List<Equipment>();
    }

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.ToList());

    public Task<List<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_equipment.Where(e => e.IsAvailable).ToList());
}
