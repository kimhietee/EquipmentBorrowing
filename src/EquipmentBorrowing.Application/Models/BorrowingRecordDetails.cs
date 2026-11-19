using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Models;

public record BorrowingRecordDetails(
    int BorrowingId,
    int StudentId,
    string StudentNumber,
    string StudentName,
    int EquipmentId,
    string EquipmentIdentifier,
    string EquipmentName,
    DateTime DateBorrowed,
    DateTime ExpectedReturnDate,
    DateTime? ReturnedAt,
    BorrowingStatus Status);
