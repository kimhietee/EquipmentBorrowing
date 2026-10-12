using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingRepository : IBorrowingRepository
{
    private readonly EquipmentBorrowingDbContext _db;
    public EfBorrowingRepository(EquipmentBorrowingDbContext db) => _db = db;

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
        => await _db.Borrowings.AddAsync(borrowing, cancellationToken);

    // LINQ Query 3: number of active borrowings for a student (used by the max-borrowings rule).
    public Task<int> CountActiveBorrowingsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
        => _db.Borrowings.CountAsync(
            b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);

    // Tracked: the Return service modifies this borrowing.
    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Borrowings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
        => _db.Borrowings.AsNoTracking()
              .Where(b => b.Status == BorrowingStatus.Active)
              .ToListAsync(cancellationToken);

    // LINQ Query 2: active borrowings joined with student and equipment (read-only).
    public async Task<List<ActiveBorrowingDetails>> GetActiveBorrowingDetailsAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from b in _db.Borrowings.AsNoTracking()
            join s in _db.Students on b.StudentId equals s.Id
            join e in _db.Equipment on b.EquipmentId equals e.Id
            where b.Status == BorrowingStatus.Active
            orderby b.ExpectedReturnDate
            select new ActiveBorrowingDetails(
                b.Id, s.StudentNumber, s.Name, e.Name, b.DateBorrowed, b.ExpectedReturnDate);

        return await query.ToListAsync(cancellationToken);
    }
}
