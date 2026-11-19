using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;

namespace EquipmentBorrowing.Application.Services;

internal static class TransactionExecutor
{
    public static async Task<ServiceResult<T>> ExecuteWriteAsync<T>(
        IUnitOfWork unitOfWork,
        IPersistenceExceptionClassifier classifier,
        Func<CancellationToken, Task<ServiceResult<T>>> operation,
        string duplicateMessage,
        string concurrencyMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<T>.Fail("The operation was canceled before any changes were saved.");
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Fail(DescribePreCommitFailure(classifier.Classify(ex), duplicateMessage, concurrencyMessage));
        }

        ServiceResult<T> outcome;
        try
        {
            outcome = await operation(cancellationToken);
            if (!outcome.Success)
            {
                await TryRollbackAsync(unitOfWork);
                return outcome;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryRollbackAsync(unitOfWork);
            return ServiceResult<T>.Fail("The operation was canceled before commit. No changes were saved.");
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(unitOfWork);
            var kind = classifier.Classify(ex);
            if (kind is PersistenceFailureKind.UniqueConstraintViolation or PersistenceFailureKind.ConcurrencyConflict)
            {
                return ServiceResult<T>.Conflict(
                    kind == PersistenceFailureKind.UniqueConstraintViolation ? duplicateMessage : concurrencyMessage);
            }

            return ServiceResult<T>.Fail(DescribePreCommitFailure(kind, duplicateMessage, concurrencyMessage));
        }

        try
        {
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return outcome;
        }
        catch (Exception ex)
        {
            var kind = classifier.Classify(ex);
            if (classifier.IsConnectionLoss(ex) ||
                kind is PersistenceFailureKind.ConnectionLostOrTimeout or PersistenceFailureKind.DatabaseUnavailable)
            {
                // BR-18: Do not automatically retry or assert rollback when commit acknowledgement is lost.
                return ServiceResult<T>.Unknown(
                    "The database connection was lost while committing the transaction, so the commit outcome is unknown. " +
                    "No automatic retry was performed. Please reconnect and refresh records to verify state before proceeding.");
            }

            await TryRollbackAsync(unitOfWork);
            if (kind is PersistenceFailureKind.UniqueConstraintViolation or PersistenceFailureKind.ConcurrencyConflict)
            {
                return ServiceResult<T>.Conflict(
                    kind == PersistenceFailureKind.UniqueConstraintViolation ? duplicateMessage : concurrencyMessage);
            }

            return ServiceResult<T>.Fail(DescribePreCommitFailure(kind, duplicateMessage, concurrencyMessage));
        }
    }

    public static string DescribeReadFailure(IPersistenceExceptionClassifier classifier, Exception exception)
    {
        var kind = classifier.Classify(exception);
        return kind switch
        {
            PersistenceFailureKind.ConnectionLostOrTimeout or PersistenceFailureKind.DatabaseUnavailable
                => "Unable to reach PostgreSQL. Please verify the database service is running and click Retry.",
            _ => "A database error occurred while loading records. Please click Retry."
        };
    }

    private static string DescribePreCommitFailure(
        PersistenceFailureKind kind,
        string duplicateMessage,
        string concurrencyMessage)
        => kind switch
        {
            PersistenceFailureKind.UniqueConstraintViolation => duplicateMessage,
            PersistenceFailureKind.ConcurrencyConflict => concurrencyMessage,
            PersistenceFailureKind.ForeignKeyViolation => "A referenced record is missing or invalid. Please refresh and try again.",
            PersistenceFailureKind.ConnectionLostOrTimeout or PersistenceFailureKind.DatabaseUnavailable
                => "Unable to reach PostgreSQL before commit. No changes were saved; please verify the connection and retry.",
            _ => "A database failure occurred before commit. No changes were saved; please refresh and retry."
        };

    private static async Task TryRollbackAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore rollback errors after a pre-commit failure.
        }
    }
}
