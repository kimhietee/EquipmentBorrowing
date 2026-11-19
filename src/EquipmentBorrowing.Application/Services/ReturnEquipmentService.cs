using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPersistenceExceptionClassifier _classifier;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository,
        IUnitOfWork unitOfWork,
        IPersistenceExceptionClassifier? classifier = null)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _classifier = classifier ?? DefaultPersistenceExceptionClassifier.Instance;
    }

    public async Task<ReturnResult> ReturnAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        if (borrowingId <= 0)
            return ReturnResult.Fail("Please select a valid active borrowing to return.");

        var result = await TransactionExecutor.ExecuteWriteAsync<Borrowing>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId, ct);
                if (borrowing is null)
                    return ServiceResult<Borrowing>.Conflict("Borrowing record no longer exists. Please refresh the list.");

                if (borrowing.Status == BorrowingStatus.Returned)
                    return ServiceResult<Borrowing>.Validation("This borrowing has already been returned.");

                var returnedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                if (returnedAt.Date < borrowing.DateBorrowed.Date)
                    return ServiceResult<Borrowing>.Validation("Return date cannot precede the checkout date.");

                borrowing.MarkAsReturned(returnedAt);

                var equipment = await _equipmentRepository.GetByIdAsync(borrowing.EquipmentId, ct);
                if (equipment is null)
                    return ServiceResult<Borrowing>.Conflict("Associated equipment record is missing. Please refresh.");

                equipment.MarkAsAvailable();
                return ServiceResult<Borrowing>.Ok(borrowing, "Equipment returned.");
            },
            duplicateMessage: "A conflicting update occurred while returning equipment. Please refresh.",
            concurrencyMessage: "This borrowing was modified concurrently. Please refresh and check its status.",
            cancellationToken);

        return result.Success
            ? ReturnResult.Ok(result.Data!, result.Message)
            : ReturnResult.Fail(result.Message, result.Status);
    }
}
