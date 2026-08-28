using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record BorrowResult(bool Success, string Message, Borrowing? Borrowing = null)
{
    public static BorrowResult Fail(string message) => new(false, message);
    public static BorrowResult Ok(Borrowing borrowing) => new(true, "Borrowing approved.", borrowing);
}