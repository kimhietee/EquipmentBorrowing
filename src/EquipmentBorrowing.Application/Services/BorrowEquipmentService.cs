using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPersistenceExceptionClassifier _classifier;
    private readonly int _maxActiveBorrowingsPerStudent;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        IUnitOfWork unitOfWork,
        int maxActiveBorrowingsPerStudent = 3,
        IPersistenceExceptionClassifier? classifier = null)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
        _unitOfWork = unitOfWork;
        _maxActiveBorrowingsPerStudent = maxActiveBorrowingsPerStudent;
        _classifier = classifier ?? DefaultPersistenceExceptionClassifier.Instance;
    }

    public Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
        => BorrowAsync(studentId, equipmentId, (DateTime?)expectedReturnDate, cancellationToken);

    public async Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateTime? expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0)
            return BorrowResult.Fail("Please select a valid student.");
        if (equipmentId <= 0)
            return BorrowResult.Fail("Please select a valid equipment item.");

        var checkoutTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var normalizedExpected = expectedReturnDate.HasValue
            ? DateTime.SpecifyKind(expectedReturnDate.Value, DateTimeKind.Unspecified)
            : (DateTime?)null;
        if (!IsValidExpectedReturnDate(checkoutTime, normalizedExpected, out var dateError))
            return BorrowResult.Fail(dateError);

        var result = await TransactionExecutor.ExecuteWriteAsync<Borrowing>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var student = await _studentRepository.GetByIdAsync(studentId, ct);
                if (student is null)
                    return ServiceResult<Borrowing>.Conflict("Selected student no longer exists. Please refresh and reselect.");

                if (!student.IsAllowedToBorrow)
                    return ServiceResult<Borrowing>.Validation("Student is not currently allowed to borrow equipment.");

                var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, ct);
                if (equipment is null)
                    return ServiceResult<Borrowing>.Conflict("Selected equipment no longer exists. Please refresh and reselect.");

                if (!equipment.IsAvailable || await _borrowingRepository.HasActiveBorrowingForEquipmentAsync(equipmentId, ct))
                    return ServiceResult<Borrowing>.Conflict("Equipment is not currently available.");

                var activeCount = await _borrowingRepository.CountActiveBorrowingsByStudentAsync(studentId, ct);
                if (activeCount >= _maxActiveBorrowingsPerStudent)
                    return ServiceResult<Borrowing>.Validation("Student has reached the maximum number of active borrowings.");

                equipment.MarkAsBorrowed();

                var borrowing = new Borrowing(
                    id: 0,
                    studentId: studentId,
                    equipmentId: equipmentId,
                    dateBorrowed: checkoutTime,
                    expectedReturnDate: normalizedExpected!.Value);

                await _borrowingRepository.AddAsync(borrowing, ct);
                return ServiceResult<Borrowing>.Ok(borrowing, "Borrowing approved.");
            },
            duplicateMessage: "This equipment already has an active borrowing. Please refresh and try again.",
            concurrencyMessage: "Equipment or student state changed concurrently. Please refresh and try again.",
            cancellationToken);

        return result.Success
            ? BorrowResult.Ok(result.Data!, result.Message)
            : BorrowResult.Fail(result.Message, result.Status);
    }

    public async Task<ServiceResult<IReadOnlyList<Borrowing>>> CheckoutMultipleAsync(
        int studentId,
        IReadOnlyList<int>? equipmentIds,
        DateTime? expectedReturnDate,
        CancellationToken cancellationToken = default,
        Func<CancellationToken, Task>? beforeCommitHook = null)
    {
        if (studentId <= 0)
            return ServiceResult<IReadOnlyList<Borrowing>>.Validation("Please select a student.");

        if (equipmentIds is null || equipmentIds.Count < 2)
            return ServiceResult<IReadOnlyList<Borrowing>>.Validation("Multi-item checkout requires at least two distinct equipment items.");

        if (equipmentIds.Any(id => id <= 0))
            return ServiceResult<IReadOnlyList<Borrowing>>.Validation("One or more selected equipment items are invalid.");

        if (equipmentIds.Distinct().Count() != equipmentIds.Count)
            return ServiceResult<IReadOnlyList<Borrowing>>.Validation("Duplicate equipment items are not allowed in a multi-item checkout.");

        var checkoutTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var normalizedExpected = expectedReturnDate.HasValue
            ? DateTime.SpecifyKind(expectedReturnDate.Value, DateTimeKind.Unspecified)
            : (DateTime?)null;
        if (!IsValidExpectedReturnDate(checkoutTime, normalizedExpected, out var dateError))
            return ServiceResult<IReadOnlyList<Borrowing>>.Validation(dateError);

        return await TransactionExecutor.ExecuteWriteAsync<IReadOnlyList<Borrowing>>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var student = await _studentRepository.GetByIdAsync(studentId, ct);
                if (student is null)
                    return ServiceResult<IReadOnlyList<Borrowing>>.Conflict("Selected student no longer exists. Please refresh and reselect.");

                if (!student.IsAllowedToBorrow)
                    return ServiceResult<IReadOnlyList<Borrowing>>.Validation("Student is not currently allowed to borrow equipment.");

                var activeCount = await _borrowingRepository.CountActiveBorrowingsByStudentAsync(studentId, ct);
                if (activeCount + equipmentIds.Count > _maxActiveBorrowingsPerStudent)
                {
                    return ServiceResult<IReadOnlyList<Borrowing>>.Validation(
                        $"Borrowing {equipmentIds.Count} item(s) exceeds the active limit of {_maxActiveBorrowingsPerStudent} " +
                        $"(student currently has {activeCount} active borrowing(s)).");
                }

                var loadedItems = await _equipmentRepository.GetByIdsAsync(equipmentIds, ct);
                var itemsById = loadedItems.ToDictionary(e => e.Id);

                foreach (var id in equipmentIds)
                {
                    if (!itemsById.TryGetValue(id, out var equipment))
                        return ServiceResult<IReadOnlyList<Borrowing>>.Conflict($"Equipment ID {id} no longer exists. Please refresh.");

                    if (!equipment.IsAvailable || await _borrowingRepository.HasActiveBorrowingForEquipmentAsync(id, ct))
                        return ServiceResult<IReadOnlyList<Borrowing>>.Conflict($"Equipment '{equipment.Name}' ({equipment.Identifier}) is no longer available.");
                }

                var borrowings = new List<Borrowing>(equipmentIds.Count);
                foreach (var id in equipmentIds)
                {
                    var equipment = itemsById[id];
                    equipment.MarkAsBorrowed();
                    borrowings.Add(new Borrowing(
                        id: 0,
                        studentId: studentId,
                        equipmentId: id,
                        dateBorrowed: checkoutTime,
                        expectedReturnDate: normalizedExpected!.Value));
                }

                await _borrowingRepository.AddRangeAsync(borrowings, ct);

                if (beforeCommitHook is not null)
                {
                    // Persist staged rows inside the open transaction before invoking the test/diagnostic hook.
                    await _unitOfWork.SaveChangesAsync(ct);
                    await beforeCommitHook(ct);
                }

                return ServiceResult<IReadOnlyList<Borrowing>>.Ok(
                    borrowings,
                    $"Checked out {borrowings.Count} equipment items together.");
            },
            duplicateMessage: "One of the selected items already has an active borrowing. No items were checked out.",
            concurrencyMessage: "Equipment or student state changed concurrently. No items were checked out; please refresh.",
            cancellationToken);
    }

    private static bool IsValidExpectedReturnDate(DateTime checkoutTime, DateTime? expectedReturnDate, out string error)
    {
        if (!expectedReturnDate.HasValue)
        {
            error = "Expected return date is required.";
            return false;
        }

        if (expectedReturnDate.Value.Date <= checkoutTime.Date)
        {
            error = "Expected return date must be later than today's checkout date.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
