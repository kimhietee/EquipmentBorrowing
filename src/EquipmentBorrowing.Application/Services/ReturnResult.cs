using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record ReturnResult(bool Success, string Message, Borrowing? Borrowing = null)
{
    public static ReturnResult Fail(string message) => new(false, message);
    public static ReturnResult Ok(Borrowing borrowing) => new(true, "Equipment returned.", borrowing);
}