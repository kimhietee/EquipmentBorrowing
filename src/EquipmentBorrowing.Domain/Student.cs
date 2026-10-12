namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; }
    public string Name { get; private set; }
    public bool IsAllowedToBorrow { get; private set; }

    public Student(int id, string studentNumber, string name, bool isAllowedToBorrow = true)
    {
        Id = id;
        StudentNumber = studentNumber;
        Name = name;
        IsAllowedToBorrow = isAllowedToBorrow;
    }
}