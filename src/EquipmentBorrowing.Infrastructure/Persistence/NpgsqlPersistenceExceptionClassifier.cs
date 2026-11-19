using System.Net.Sockets;
using EquipmentBorrowing.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class NpgsqlPersistenceExceptionClassifier : IPersistenceExceptionClassifier
{
    public PersistenceFailureKind Classify(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
                return PersistenceFailureKind.ConcurrencyConflict;

            if (current is PostgresException pg)
            {
                return pg.SqlState switch
                {
                    PostgresErrorCodes.UniqueViolation => PersistenceFailureKind.UniqueConstraintViolation,
                    PostgresErrorCodes.ForeignKeyViolation => PersistenceFailureKind.ForeignKeyViolation,
                    _ when pg.SqlState.StartsWith("08", StringComparison.Ordinal) ||
                           pg.SqlState == PostgresErrorCodes.AdminShutdown ||
                           pg.SqlState == PostgresErrorCodes.CrashShutdown ||
                           pg.SqlState == PostgresErrorCodes.CannotConnectNow
                        => PersistenceFailureKind.DatabaseUnavailable,
                    _ => PersistenceFailureKind.Other
                };
            }

            if (current is NpgsqlException npgsqlEx)
            {
                if (npgsqlEx.InnerException is TimeoutException or IOException or SocketException or EndOfStreamException)
                    return PersistenceFailureKind.ConnectionLostOrTimeout;

                return npgsqlEx.IsTransient
                    ? PersistenceFailureKind.ConnectionLostOrTimeout
                    : PersistenceFailureKind.DatabaseUnavailable;
            }

            if (current is TimeoutException or IOException or SocketException or EndOfStreamException)
                return PersistenceFailureKind.ConnectionLostOrTimeout;
        }

        return PersistenceFailureKind.Other;
    }

    public bool IsConnectionLoss(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or IOException or SocketException or EndOfStreamException)
                return true;

            if (current is PostgresException pg &&
                (pg.SqlState.StartsWith("08", StringComparison.Ordinal) ||
                 pg.SqlState == PostgresErrorCodes.AdminShutdown ||
                 pg.SqlState == PostgresErrorCodes.CrashShutdown))
                return true;

            if (current is NpgsqlException npgsqlEx &&
                npgsqlEx is not PostgresException &&
                (npgsqlEx.IsTransient || npgsqlEx.InnerException is TimeoutException or IOException or SocketException))
                return true;
        }

        return false;
    }
}
