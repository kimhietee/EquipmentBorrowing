using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record BorrowResult(
    bool Success,
    string Message,
    Borrowing? Borrowing = null,
    OperationStatus Status = OperationStatus.ValidationFailed)
{
    public bool IsUnknownOutcome => Status == OperationStatus.UnknownOutcome;

    public static BorrowResult Fail(string message, OperationStatus status = OperationStatus.ValidationFailed)
        => new(false, message, null, status);

    public static BorrowResult Ok(Borrowing borrowing, string message = "Borrowing approved.")
        => new(true, message, borrowing, OperationStatus.Success);
}
