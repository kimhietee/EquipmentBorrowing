namespace EquipmentBorrowing.Application.Models;

public record ServiceResult(OperationStatus Status, string Message)
{
    public bool Success => Status == OperationStatus.Success;
    public bool IsUnknownOutcome => Status == OperationStatus.UnknownOutcome;

    public static ServiceResult Ok(string message) => new(OperationStatus.Success, message);
    public static ServiceResult Validation(string message) => new(OperationStatus.ValidationFailed, message);
    public static ServiceResult Conflict(string message) => new(OperationStatus.ConflictOrMissing, message);
    public static ServiceResult Fail(string message) => new(OperationStatus.Failure, message);
    public static ServiceResult Unknown(string message) => new(OperationStatus.UnknownOutcome, message);
}

public record ServiceResult<T>(OperationStatus Status, string Message, T? Data = default)
{
    public bool Success => Status == OperationStatus.Success;
    public bool IsUnknownOutcome => Status == OperationStatus.UnknownOutcome;

    public static ServiceResult<T> Ok(T data, string message = "Success.")
        => new(OperationStatus.Success, message, data);

    public static ServiceResult<T> Validation(string message)
        => new(OperationStatus.ValidationFailed, message);

    public static ServiceResult<T> Conflict(string message)
        => new(OperationStatus.ConflictOrMissing, message);

    public static ServiceResult<T> Fail(string message)
        => new(OperationStatus.Failure, message);

    public static ServiceResult<T> Unknown(string message)
        => new(OperationStatus.UnknownOutcome, message);
}
