namespace EquipmentBorrowing.Application.Models;

public record ActiveBorrowingDetails(
    int BorrowingId,
    string StudentNumber,
    string StudentName,
    string EquipmentIdentifier,
    string EquipmentName,
    DateTime DateBorrowed,
    DateTime ExpectedReturnDate);
