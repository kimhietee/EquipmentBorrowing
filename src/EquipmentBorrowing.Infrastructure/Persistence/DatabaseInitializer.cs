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
            new Student(0, "2026-0001", "Ana Reyes", true),
            new Student(0, "2026-0002", "Juan Dela Cruz", true),
            new Student(0, "2026-0003", "Maria Santos", false)
        };

        var equipment = new[]
        {
            new Equipment(0, "Laptop 01", "Laptop"),
            new Equipment(0, "Laptop 02", "Laptop"),
            new Equipment(0, "Digital Multimeter", "Instrument"),
            new Equipment(0, "Oscilloscope", "Instrument"),
            new Equipment(0, "Arduino Kit 01", "Microcontroller"),
            new Equipment(0, "Projector 01", "Projector")
        };

        db.Students.AddRange(students);
        db.Equipment.AddRange(equipment);
        await db.SaveChangesAsync();

        equipment[0].MarkAsBorrowed();
        db.Borrowings.Add(new Borrowing(
            0, students[0].Id, equipment[0].Id,
            DateTime.Now.AddDays(-2), DateTime.Now.AddDays(5)));
        await db.SaveChangesAsync();
    }
}
