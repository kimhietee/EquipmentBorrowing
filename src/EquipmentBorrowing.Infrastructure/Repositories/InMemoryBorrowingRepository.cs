using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();
    private readonly IStudentRepository? _students;
    private readonly IEquipmentRepository? _equipment;
    private int _nextId = 1;

    public InMemoryBorrowingRepository(
        IStudentRepository? students = null,
        IEquipmentRepository? equipment = null)
    {
        _students = students;
        _equipment = equipment;
    }

    public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        AssignIdIfZero(borrowing);
        _borrowings.Add(borrowing);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Borrowing> borrowings, CancellationToken cancellationToken = default)
    {
        foreach (var borrowing in borrowings)
        {
            AssignIdIfZero(borrowing);
            _borrowings.Add(borrowing);
        }
        return Task.CompletedTask;
    }

    public Task<int> CountActiveBorrowingsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
        => Task.FromResult(_borrowings.Count(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active));

    public Task<bool> HasActiveBorrowingForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(_borrowings.Any(b => b.EquipmentId == equipmentId && b.Status == BorrowingStatus.Active));

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_borrowings.FirstOrDefault(b => b.Id == id));

    public Task<List<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_borrowings.Where(b => b.Status == BorrowingStatus.Active).ToList());

    public async Task<List<ActiveBorrowingDetails>> GetActiveBorrowingDetailsAsync(CancellationToken cancellationToken = default)
    {
        var details = await ProjectAllAsync(cancellationToken);
        return details
            .Where(r => r.Status == BorrowingStatus.Active)
            .OrderBy(r => r.ExpectedReturnDate)
            .ThenBy(r => r.BorrowingId)
            .Select(r => new ActiveBorrowingDetails(
                r.BorrowingId,
                r.StudentNumber,
                r.StudentName,
                r.EquipmentIdentifier,
                r.EquipmentName,
                r.DateBorrowed,
                r.ExpectedReturnDate))
            .ToList();
    }

    public async Task<List<BorrowingRecordDetails>> GetStudentHistoryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var details = await ProjectAllAsync(cancellationToken);
        return details
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.DateBorrowed)
            .ThenBy(r => r.BorrowingId)
            .ToList();
    }

    public async Task<List<BorrowingRecordDetails>> GetEquipmentHistoryAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        var details = await ProjectAllAsync(cancellationToken);
        return details
            .Where(r => r.EquipmentId == equipmentId)
            .OrderByDescending(r => r.DateBorrowed)
            .ThenBy(r => r.BorrowingId)
            .ToList();
    }

    public async Task<List<BorrowingRecordDetails>> SearchBorrowingsAsync(
        BorrowingSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<BorrowingRecordDetails> details = await ProjectAllAsync(cancellationToken);

        if (query.StatusFilter == BorrowingStatusFilter.Active)
            details = details.Where(r => r.Status == BorrowingStatus.Active);
        else if (query.StatusFilter == BorrowingStatusFilter.Returned)
            details = details.Where(r => r.Status == BorrowingStatus.Returned);

        var trimmed = query.SearchText?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            details = details.Where(r =>
                r.StudentName.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                r.StudentNumber.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                r.EquipmentName.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = (query.SortField, query.SortDirection) switch
        {
            (BorrowingSortField.DateBorrowed, SortDirection.Ascending)
                => details.OrderBy(r => r.DateBorrowed).ThenBy(r => r.BorrowingId),
            (BorrowingSortField.DateBorrowed, SortDirection.Descending)
                => details.OrderByDescending(r => r.DateBorrowed).ThenBy(r => r.BorrowingId),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Ascending)
                => details.OrderBy(r => r.ExpectedReturnDate).ThenBy(r => r.BorrowingId),
            _
                => details.OrderByDescending(r => r.ExpectedReturnDate).ThenBy(r => r.BorrowingId)
        };

        return ordered.ToList();
    }

    private async Task<List<BorrowingRecordDetails>> ProjectAllAsync(CancellationToken cancellationToken)
    {
        if (_students is null || _equipment is null)
            throw new NotSupportedException("Provide student and equipment repositories to project borrowing details in memory.");

        var students = (await _students.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id);
        var equipment = (await _equipment.GetAllAsync(cancellationToken)).ToDictionary(e => e.Id);

        return _borrowings
            .Where(b => students.ContainsKey(b.StudentId) && equipment.ContainsKey(b.EquipmentId))
            .Select(b =>
            {
                var s = students[b.StudentId];
                var e = equipment[b.EquipmentId];
                return new BorrowingRecordDetails(
                    b.Id,
                    s.Id,
                    s.StudentNumber,
                    s.Name,
                    e.Id,
                    e.Identifier,
                    e.Name,
                    b.DateBorrowed,
                    b.ExpectedReturnDate,
                    b.ReturnedAt,
                    b.Status);
            })
            .ToList();
    }

    private void AssignIdIfZero(Borrowing borrowing)
    {
        if (borrowing.Id == 0)
        {
            typeof(Borrowing).GetProperty(nameof(Borrowing.Id))!.SetValue(borrowing, _nextId++);
        }
    }
}
