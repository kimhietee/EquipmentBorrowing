namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public bool IsAvailable { get; private set; }

    public Equipment(int id, string name, string type, bool isAvailable = true)
    {
        Id = id;
        Name = name;
        Type = type;
        IsAvailable = isAvailable;
    }

    public void MarkAsBorrowed()
    {
        if (!IsAvailable)
            throw new InvalidOperationException($"Equipment '{Name}' is already borrowed.");
        IsAvailable = false;
    }

    public void MarkAsAvailable() => IsAvailable = true;
}