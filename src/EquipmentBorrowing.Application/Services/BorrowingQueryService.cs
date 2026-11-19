using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowingQueryService
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IPersistenceExceptionClassifier _classifier;

    public BorrowingQueryService(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        IBorrowingRepository borrowingRepository,
        IPersistenceExceptionClassifier? classifier = null)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowingRepository = borrowingRepository;
        _classifier = classifier ?? DefaultPersistenceExceptionClassifier.Instance;
    }

    public async Task<ServiceResult<List<Equipment>>> GetEquipmentCatalogAsync(
        bool availableOnly,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = availableOnly
                ? await _equipmentRepository.GetAvailableAsync(cancellationToken)
                : await _equipmentRepository.GetAllAsync(cancellationToken);

            return ServiceResult<List<Equipment>>.Ok(items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<Equipment>>.Fail("Loading equipment was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<Equipment>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }

    public async Task<ServiceResult<List<Student>>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var students = await _studentRepository.GetAllAsync(cancellationToken);
            return ServiceResult<List<Student>>.Ok(students);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<Student>>.Fail("Loading students was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<Student>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }

    public async Task<ServiceResult<List<ActiveBorrowingDetails>>> GetActiveBorrowingsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var records = await _borrowingRepository.GetActiveBorrowingDetailsAsync(cancellationToken);
            return ServiceResult<List<ActiveBorrowingDetails>>.Ok(records);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<ActiveBorrowingDetails>>.Fail("Loading active borrowings was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<ActiveBorrowingDetails>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }

    public async Task<ServiceResult<List<BorrowingRecordDetails>>> GetStudentHistoryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0)
            return ServiceResult<List<BorrowingRecordDetails>>.Validation("Please select a student.");

        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
            if (student is null)
                return ServiceResult<List<BorrowingRecordDetails>>.Conflict("Selected student no longer exists. Please refresh and select another student.");

            var history = await _borrowingRepository.GetStudentHistoryAsync(studentId, cancellationToken);
            return ServiceResult<List<BorrowingRecordDetails>>.Ok(history);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail("Loading student history was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }

    public async Task<ServiceResult<List<BorrowingRecordDetails>>> GetEquipmentHistoryAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        if (equipmentId <= 0)
            return ServiceResult<List<BorrowingRecordDetails>>.Validation("Please select an equipment item.");

        try
        {
            var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
            if (equipment is null)
                return ServiceResult<List<BorrowingRecordDetails>>.Conflict("Selected equipment item no longer exists. Please refresh and select another item.");

            var history = await _borrowingRepository.GetEquipmentHistoryAsync(equipmentId, cancellationToken);
            return ServiceResult<List<BorrowingRecordDetails>>.Ok(history);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail("Loading equipment history was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }

    public async Task<ServiceResult<List<BorrowingRecordDetails>>> SearchBorrowingsAsync(
        BorrowingSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _borrowingRepository.SearchBorrowingsAsync(query, cancellationToken);
            return ServiceResult<List<BorrowingRecordDetails>>.Ok(results);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail("Searching borrowings was canceled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<BorrowingRecordDetails>>.Fail(TransactionExecutor.DescribeReadFailure(_classifier, ex));
        }
    }
}
