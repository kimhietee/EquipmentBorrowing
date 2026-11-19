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

    public async Task AddRangeAsync(IEnumerable<Borrowing> borrowings, CancellationToken cancellationToken = default)
        => await _db.Borrowings.AddRangeAsync(borrowings, cancellationToken);

    public Task<int> CountActiveBorrowingsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
        => _db.Borrowings.CountAsync(
            b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);

    public Task<bool> HasActiveBorrowingForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default)
        => _db.Borrowings.AnyAsync(
            b => b.EquipmentId == equipmentId && b.Status == BorrowingStatus.Active, cancellationToken);

    public Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Borrowings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
        => _db.Borrowings.AsNoTracking()
              .Where(b => b.Status == BorrowingStatus.Active)
              .ToListAsync(cancellationToken);

    public Task<List<ActiveBorrowingDetails>> GetActiveBorrowingDetailsAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from b in _db.Borrowings.AsNoTracking()
            join s in _db.Students.AsNoTracking() on b.StudentId equals s.Id
            join e in _db.Equipment.AsNoTracking() on b.EquipmentId equals e.Id
            where b.Status == BorrowingStatus.Active
            orderby b.ExpectedReturnDate, b.Id
            select new ActiveBorrowingDetails(
                b.Id, s.StudentNumber, s.Name, e.Identifier, e.Name, b.DateBorrowed, b.ExpectedReturnDate);

        return query.ToListAsync(cancellationToken);
    }

    public Task<List<BorrowingRecordDetails>> GetStudentHistoryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var query = BaseJoinedQuery()
            .Where(x => x.B.StudentId == studentId)
            .OrderByDescending(x => x.B.DateBorrowed)
            .ThenBy(x => x.B.Id);

        return ProjectToDetails(query).ToListAsync(cancellationToken);
    }

    public Task<List<BorrowingRecordDetails>> GetEquipmentHistoryAsync(int equipmentId, CancellationToken cancellationToken = default)
    {
        var query = BaseJoinedQuery()
            .Where(x => x.B.EquipmentId == equipmentId)
            .OrderByDescending(x => x.B.DateBorrowed)
            .ThenBy(x => x.B.Id);

        return ProjectToDetails(query).ToListAsync(cancellationToken);
    }

    public Task<List<BorrowingRecordDetails>> SearchBorrowingsAsync(
        BorrowingSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = BaseJoinedQuery();

        if (query.StatusFilter == BorrowingStatusFilter.Active)
            rows = rows.Where(x => x.B.Status == BorrowingStatus.Active);
        else if (query.StatusFilter == BorrowingStatusFilter.Returned)
            rows = rows.Where(x => x.B.Status == BorrowingStatus.Returned);

        var trimmed = query.SearchText?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            var escaped = EscapeLikePattern(trimmed);
            var pattern = $"%{escaped}%";
            rows = rows.Where(x =>
                EF.Functions.ILike(x.S.Name, pattern, "\\") ||
                EF.Functions.ILike(x.S.StudentNumber, pattern, "\\") ||
                EF.Functions.ILike(x.E.Name, pattern, "\\"));
        }

        var ordered = (query.SortField, query.SortDirection) switch
        {
            (BorrowingSortField.DateBorrowed, SortDirection.Ascending)
                => rows.OrderBy(x => x.B.DateBorrowed).ThenBy(x => x.B.Id),
            (BorrowingSortField.DateBorrowed, SortDirection.Descending)
                => rows.OrderByDescending(x => x.B.DateBorrowed).ThenBy(x => x.B.Id),
            (BorrowingSortField.ExpectedReturnDate, SortDirection.Ascending)
                => rows.OrderBy(x => x.B.ExpectedReturnDate).ThenBy(x => x.B.Id),
            _
                => rows.OrderByDescending(x => x.B.ExpectedReturnDate).ThenBy(x => x.B.Id)
        };

        return ProjectToDetails(ordered).ToListAsync(cancellationToken);
    }

    private IQueryable<BorrowingJoinRow> BaseJoinedQuery()
        => from b in _db.Borrowings.AsNoTracking()
           join s in _db.Students.AsNoTracking() on b.StudentId equals s.Id
           join e in _db.Equipment.AsNoTracking() on b.EquipmentId equals e.Id
           select new BorrowingJoinRow { B = b, S = s, E = e };

    private static IQueryable<BorrowingRecordDetails> ProjectToDetails(IQueryable<BorrowingJoinRow> query)
        => query.Select(x => new BorrowingRecordDetails(
            x.B.Id,
            x.S.Id,
            x.S.StudentNumber,
            x.S.Name,
            x.E.Id,
            x.E.Identifier,
            x.E.Name,
            x.B.DateBorrowed,
            x.B.ExpectedReturnDate,
            x.B.ReturnedAt,
            x.B.Status));

    private static string EscapeLikePattern(string input)
        => input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class BorrowingJoinRow
    {
        public Borrowing B { get; init; } = null!;
        public Student S { get; init; } = null!;
        public Equipment E { get; init; } = null!;
    }
}
