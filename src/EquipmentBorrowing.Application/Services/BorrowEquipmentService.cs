using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly int _maxActiveBorrowingsPerStudent;

    private static int _nextBorrowingId = 1;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        int maxActiveBorrowingsPerStudent = 3)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
        _maxActiveBorrowingsPerStudent = maxActiveBorrowingsPerStudent;
    }

    public async Task<BorrowResult> BorrowAsync(
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return BorrowResult.Fail("Student does not exist.");

        if (!student.IsAllowedToBorrow)
            return BorrowResult.Fail("Student is not currently allowed to borrow equipment.");

        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return BorrowResult.Fail("Equipment does not exist.");

        if (!equipment.IsAvailable)
            return BorrowResult.Fail("Equipment is not currently available.");

        var activeCount = await _borrowingRepository.CountActiveBorrowingsByStudentAsync(studentId, cancellationToken);
        if (activeCount >= _maxActiveBorrowingsPerStudent)
            return BorrowResult.Fail("Student has reached the maximum number of active borrowings.");

        equipment.MarkAsBorrowed();

        var borrowing = new Borrowing(
            id: _nextBorrowingId++,
            studentId: studentId,
            equipmentId: equipmentId,
            dateBorrowed: DateTime.Now,
            expectedReturnDate: expectedReturnDate);

        await _borrowingRepository.AddAsync(borrowing, cancellationToken);

        return BorrowResult.Ok(borrowing);
    }
}