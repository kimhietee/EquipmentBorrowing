namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string Identifier { get; private set; }
    public string NormalizedIdentifier { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }

    private Equipment()
    {
        Identifier = string.Empty;
        NormalizedIdentifier = string.Empty;
        Name = string.Empty;
    }

    public Equipment(
        int id,
        string identifier,
        string name,
        string? description = null,
        bool isAvailable = true)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Equipment identifier is required.", nameof(identifier));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name is required.", nameof(name));

        Id = id;
        Identifier = identifier.Trim();
        NormalizedIdentifier = NormalizeIdentifier(Identifier);
        Name = name.Trim();
        Description = NormalizeOptionalDescription(description);
        IsAvailable = isAvailable;
    }

    public static string NormalizeIdentifier(string identifier)
        => identifier.Trim().ToUpperInvariant();

    public static string? NormalizeOptionalDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void UpdateDetails(string identifier, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Equipment identifier is required.", nameof(identifier));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Equipment name is required.", nameof(name));

        Identifier = identifier.Trim();
        NormalizedIdentifier = NormalizeIdentifier(Identifier);
        Name = name.Trim();
        Description = NormalizeOptionalDescription(description);
    }

    public void MarkAsBorrowed()
    {
        if (!IsAvailable)
            throw new InvalidOperationException($"Equipment '{Name}' ({Identifier}) is already borrowed.");
        IsAvailable = false;
    }

    public void MarkAsAvailable() => IsAvailable = true;
}
