namespace EquipmentBorrowing.Application.Models;

public record ActiveBorrowingDetails(
    int BorrowingId,
    string StudentNumber,
    string StudentName,
    string EquipmentName,
    DateTime DateBorrowed,
    DateTime ExpectedReturnDate);
