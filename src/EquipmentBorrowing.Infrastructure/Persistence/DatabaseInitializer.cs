using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(EquipmentBorrowingDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Students.AnyAsync())
            return;

        var students = new[]
        {
            new Student(0, "2026-0001", "Ana Reyes", isAllowedToBorrow: true, contact: "0917-100-0001"),
            new Student(0, "2026-0002", "Juan Dela Cruz", isAllowedToBorrow: true, contact: "juan.delacruz@campus.edu"),
            new Student(0, "2026-0003", "Maria Santos", isAllowedToBorrow: false, contact: null)
        };

        var equipment = new[]
        {
            new Equipment(0, "EQ-001", "Laptop 01", "Laptop"),
            new Equipment(0, "EQ-002", "Laptop 02", "Laptop"),
            new Equipment(0, "EQ-003", "Digital Multimeter", "Instrument"),
            new Equipment(0, "EQ-004", "Oscilloscope", "Instrument"),
            new Equipment(0, "EQ-005", "Arduino Kit 01", "Microcontroller"),
            new Equipment(0, "EQ-006", "Projector 01", "Projector")
        };

        db.Students.AddRange(students);
        db.Equipment.AddRange(equipment);
        await db.SaveChangesAsync();

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        equipment[0].MarkAsBorrowed();
        var activeBorrowing = new Borrowing(
            0, students[0].Id, equipment[0].Id,
            now.AddDays(-2), now.AddDays(5));

        var returnedBorrowing = new Borrowing(
            0, students[1].Id, equipment[2].Id,
            now.AddDays(-7), now.AddDays(-2));
        returnedBorrowing.MarkAsReturned(now.AddDays(-3));

        db.Borrowings.AddRange(activeBorrowing, returnedBorrowing);
        await db.SaveChangesAsync();
    }
}
