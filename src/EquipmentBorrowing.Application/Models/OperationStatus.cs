namespace EquipmentBorrowing.Application.Models;

public enum OperationStatus
{
    Success,
    ValidationFailed,
    ConflictOrMissing,
    Failure,
    UnknownOutcome
}
