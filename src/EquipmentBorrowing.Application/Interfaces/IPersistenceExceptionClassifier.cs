namespace EquipmentBorrowing.Application.Interfaces;

public enum PersistenceFailureKind
{
    UniqueConstraintViolation,
    ForeignKeyViolation,
    ConcurrencyConflict,
    ConnectionLostOrTimeout,
    DatabaseUnavailable,
    Other
}

public interface IPersistenceExceptionClassifier
{
    PersistenceFailureKind Classify(Exception exception);
    bool IsConnectionLoss(Exception exception);
}

public sealed class DefaultPersistenceExceptionClassifier : IPersistenceExceptionClassifier
{
    public static readonly DefaultPersistenceExceptionClassifier Instance = new();

    public PersistenceFailureKind Classify(Exception exception)
    {
        if (IsConnectionLoss(exception))
            return PersistenceFailureKind.ConnectionLostOrTimeout;
        return PersistenceFailureKind.Other;
    }

    public bool IsConnectionLoss(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or IOException or System.Net.Sockets.SocketException)
                return true;
        }
        return false;
    }
}
