using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
    }

    public async Task<ReturnResult> ReturnAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing is null)
            return ReturnResult.Fail("Borrowing record not found.");

        if (borrowing.Status == Domain.BorrowingStatus.Returned)
            return ReturnResult.Fail("This borrowing has already been returned.");

        borrowing.MarkAsReturned();

        var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        equipment?.MarkAsAvailable();

        return ReturnResult.Ok(borrowing);
    }
}