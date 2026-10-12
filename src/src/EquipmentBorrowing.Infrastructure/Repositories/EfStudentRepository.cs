using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public EfStudentRepository(EquipmentBorrowingDbContext db) => _db = db;

    // Tracked: the service may modify the entity.
    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    // No tracking: display-only list.
    public Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.Students.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
}
