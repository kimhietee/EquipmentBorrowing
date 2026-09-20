using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }

    public Task<int> CountActiveBorrowingsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var count = _borrowings.Count(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active);
        return Task.FromResult(count);
    }
    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_borrowings.FirstOrDefault(b => b.Id == id));
    }
    public Task<List<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
    {
        var active = _borrowings.Where(b => b.Status == BorrowingStatus.Active).ToList();
        return Task.FromResult(active);
    }
}