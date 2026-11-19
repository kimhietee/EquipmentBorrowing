namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateTime DateBorrowed { get; private set; }
    public DateTime ExpectedReturnDate { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public BorrowingStatus Status { get; private set; }

    private Borrowing()
    {
    }

    public Borrowing(int id, int studentId, int equipmentId,
                     DateTime dateBorrowed, DateTime expectedReturnDate)
    {
        if (expectedReturnDate.Date <= dateBorrowed.Date)
            throw new ArgumentException("Expected return date must be later than the checkout date.", nameof(expectedReturnDate));

        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        DateBorrowed = dateBorrowed;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
    }

    public void MarkAsReturned(DateTime returnedAt)
    {
        if (Status == BorrowingStatus.Returned)
            throw new InvalidOperationException("Borrowing has already been marked as returned.");
        if (returnedAt.Date < DateBorrowed.Date)
            throw new InvalidOperationException("Actual return date cannot precede the checkout date.");

        Status = BorrowingStatus.Returned;
        ReturnedAt = returnedAt;
    }
}
