using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record ReturnResult(
    bool Success,
    string Message,
    Borrowing? Borrowing = null,
    OperationStatus Status = OperationStatus.ValidationFailed)
{
    public bool IsUnknownOutcome => Status == OperationStatus.UnknownOutcome;

    public static ReturnResult Fail(string message, OperationStatus status = OperationStatus.ValidationFailed)
        => new(false, message, null, status);

    public static ReturnResult Ok(Borrowing borrowing, string message = "Equipment returned.")
        => new(true, message, borrowing, OperationStatus.Success);
}
