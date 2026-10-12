using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : IEquipmentRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public EfEquipmentRepository(EquipmentBorrowingDbContext db) => _db = db;

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Equipment.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.Equipment.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken);

    public Task<List<Equipment>> GetAvailableAsync(CancellationToken cancellationToken = default)
        => _db.Equipment.AsNoTracking()
              .Where(e => e.IsAvailable)
              .OrderBy(e => e.Name)
              .ToListAsync(cancellationToken);
}
