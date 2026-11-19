using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class StudentManagementService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPersistenceExceptionClassifier _classifier;

    public StudentManagementService(
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IPersistenceExceptionClassifier? classifier = null)
    {
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _classifier = classifier ?? DefaultPersistenceExceptionClassifier.Instance;
    }

    public async Task<ServiceResult<Student>> RegisterStudentAsync(
        string? studentNumber,
        string? name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            return ServiceResult<Student>.Validation("Institutional student number is required.");
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<Student>.Validation("Student full name is required.");

        var normalizedNumber = Student.NormalizeStudentNumber(studentNumber);

        return await TransactionExecutor.ExecuteWriteAsync<Student>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var existing = await _studentRepository.FindByNormalizedStudentNumberAsync(normalizedNumber, ct);
                if (existing is not null)
                    return ServiceResult<Student>.Validation($"Student number '{studentNumber.Trim()}' is already registered.");

                var student = new Student(0, studentNumber, name, isAllowedToBorrow: true, contact: contact);
                await _studentRepository.AddAsync(student, ct);
                return ServiceResult<Student>.Ok(student, $"Student '{student.Name}' ({student.StudentNumber}) registered.");
            },
            duplicateMessage: $"Student number '{studentNumber.Trim()}' is already registered.",
            concurrencyMessage: "A concurrent update prevented registering the student. Please refresh and retry.",
            cancellationToken);
    }

    public async Task<ServiceResult<Student>> UpdateStudentAsync(
        int studentId,
        string? studentNumber,
        string? name,
        string? contact,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0)
            return ServiceResult<Student>.Validation("Please select a valid student.");
        if (string.IsNullOrWhiteSpace(studentNumber))
            return ServiceResult<Student>.Validation("Institutional student number is required.");
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<Student>.Validation("Student full name is required.");

        var normalizedNumber = Student.NormalizeStudentNumber(studentNumber);

        return await TransactionExecutor.ExecuteWriteAsync<Student>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var student = await _studentRepository.GetByIdAsync(studentId, ct);
                if (student is null)
                    return ServiceResult<Student>.Conflict("Selected student no longer exists. Please refresh.");

                var duplicate = await _studentRepository.FindByNormalizedStudentNumberAsync(normalizedNumber, ct);
                if (duplicate is not null && duplicate.Id != studentId)
                    return ServiceResult<Student>.Validation($"Student number '{studentNumber.Trim()}' is already assigned to another student.");

                student.UpdateDetails(studentNumber, name, contact);
                return ServiceResult<Student>.Ok(student, $"Student '{student.Name}' updated.");
            },
            duplicateMessage: $"Student number '{studentNumber.Trim()}' is already assigned to another student.",
            concurrencyMessage: "Student record was modified concurrently. Please refresh and retry.",
            cancellationToken);
    }

    public async Task<ServiceResult<Student>> SetBorrowingEligibilityAsync(
        int studentId,
        bool isAllowed,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0)
            return ServiceResult<Student>.Validation("Please select a valid student.");

        return await TransactionExecutor.ExecuteWriteAsync<Student>(
            _unitOfWork,
            _classifier,
            async ct =>
            {
                var student = await _studentRepository.GetByIdAsync(studentId, ct);
                if (student is null)
                    return ServiceResult<Student>.Conflict("Selected student no longer exists. Please refresh.");

                if (!student.SetBorrowingEligibility(isAllowed))
                {
                    var stateText = isAllowed ? "already permitted to borrow" : "already restricted from borrowing";
                    return ServiceResult<Student>.Ok(student, $"Student '{student.Name}' is {stateText}. No changes were made.");
                }

                var actionText = isAllowed ? "permitted to borrow" : "restricted from borrowing";
                return ServiceResult<Student>.Ok(student, $"Student '{student.Name}' is now {actionText}.");
            },
            duplicateMessage: "A database constraint prevented updating eligibility.",
            concurrencyMessage: "Student record was modified concurrently. Please refresh and retry.",
            cancellationToken);
    }
}
