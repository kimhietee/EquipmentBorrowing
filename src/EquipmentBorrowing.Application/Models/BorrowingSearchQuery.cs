namespace EquipmentBorrowing.Application.Models;

public enum BorrowingStatusFilter
{
    All,
    Active,
    Returned
}

public enum BorrowingSortField
{
    DateBorrowed,
    ExpectedReturnDate
}

public enum SortDirection
{
    Ascending,
    Descending
}

public record BorrowingSearchQuery(
    string? SearchText,
    BorrowingStatusFilter StatusFilter = BorrowingStatusFilter.All,
    BorrowingSortField SortField = BorrowingSortField.DateBorrowed,
    SortDirection SortDirection = SortDirection.Descending);
