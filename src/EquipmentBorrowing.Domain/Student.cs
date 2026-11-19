namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; }
    public string NormalizedStudentNumber { get; private set; }
    public string Name { get; private set; }
    public string? Contact { get; private set; }
    public bool IsAllowedToBorrow { get; private set; }

    private Student()   
    {
        StudentNumber = string.Empty;
        NormalizedStudentNumber = string.Empty;
        Name = string.Empty;
    }

    public Student(
        int id,
        string studentNumber,
        string name,
        bool isAllowedToBorrow = true,
        string? contact = null)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            throw new ArgumentException("Institutional student number is required.", nameof(studentNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Student full name is required.", nameof(name));

        Id = id;
        StudentNumber = studentNumber.Trim();
        NormalizedStudentNumber = NormalizeStudentNumber(StudentNumber);
        Name = name.Trim();
        Contact = NormalizeOptionalContact(contact);
        IsAllowedToBorrow = isAllowedToBorrow;
    }

    public static string NormalizeStudentNumber(string studentNumber)
        => studentNumber.Trim().ToUpperInvariant();

    public static string? NormalizeOptionalContact(string? contact)
        => string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();

    public void UpdateDetails(string studentNumber, string name, string? contact)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            throw new ArgumentException("Institutional student number is required.", nameof(studentNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Student full name is required.", nameof(name));

        StudentNumber = studentNumber.Trim();
        NormalizedStudentNumber = NormalizeStudentNumber(StudentNumber);
        Name = name.Trim();
        Contact = NormalizeOptionalContact(contact);
    }

    public bool SetBorrowingEligibility(bool isAllowed)
    {
        if (IsAllowedToBorrow == isAllowed)
            return false;

        IsAllowedToBorrow = isAllowed;
        return true;
    }
}
