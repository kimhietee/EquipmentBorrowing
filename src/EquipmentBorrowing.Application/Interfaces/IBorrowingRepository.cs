using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Borrowing> borrowings, CancellationToken cancellationToken = default);
    Task<int> CountActiveBorrowingsByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveBorrowingForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default);
    Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default);
    Task<List<ActiveBorrowingDetails>> GetActiveBorrowingDetailsAsync(CancellationToken cancellationToken = default);
    Task<List<BorrowingRecordDetails>> GetStudentHistoryAsync(int studentId, CancellationToken cancellationToken = default);
    Task<List<BorrowingRecordDetails>> GetEquipmentHistoryAsync(int equipmentId, CancellationToken cancellationToken = default);
    Task<List<BorrowingRecordDetails>> SearchBorrowingsAsync(BorrowingSearchQuery query, CancellationToken cancellationToken = default);
}
