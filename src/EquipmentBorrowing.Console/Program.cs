using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

Console.WriteLine("=== Campus Equipment Borrowing System - Demo ===\n");

// Seed some starting data, same shape as the test setup.
var students = new InMemoryStudentRepository(new[]
{
    new Student(1, "Juan Dela Cruz", isAllowedToBorrow: true),
    new Student(2, "Maria Santos", isAllowedToBorrow: false)
});

var equipment = new InMemoryEquipmentRepository(new[]
{
    new Equipment(101, "Digital Multimeter", isAvailable: true),
    new Equipment(102, "Oscilloscope", isAvailable: false)
});

var borrowings = new InMemoryBorrowingRepository();

var service = new BorrowEquipmentService(students, equipment, borrowings, maxActiveBorrowingsPerStudent: 2);

// --- Successful case ---
Console.WriteLine("Attempt 1: Juan borrows the Multimeter (should succeed)");
var result1 = await service.BorrowAsync(studentId: 1, equipmentId: 101, expectedReturnDate: DateTime.Now.AddDays(7));
PrintResult(result1);

// --- Failure case: equipment unavailable ---
Console.WriteLine("\nAttempt 2: Juan borrows the Oscilloscope (should fail - unavailable)");
var result2 = await service.BorrowAsync(studentId: 1, equipmentId: 102, expectedReturnDate: DateTime.Now.AddDays(7));
PrintResult(result2);

// --- Failure case: student not allowed ---
Console.WriteLine("\nAttempt 3: Maria borrows the Multimeter (should fail - not allowed to borrow, and it's already taken anyway)");
var result3 = await service.BorrowAsync(studentId: 2, equipmentId: 101, expectedReturnDate: DateTime.Now.AddDays(7));
PrintResult(result3);

Console.WriteLine("\nDone. Press any key to exit.");
Console.ReadKey();

static void PrintResult(BorrowResult result)
{
    Console.WriteLine($"  Success: {result.Success}");
    Console.WriteLine($"  Message: {result.Message}");
    if (result.Borrowing is not null)
    {
        Console.WriteLine($"  Borrowing ID: {result.Borrowing.Id}, Status: {result.Borrowing.Status}, Return by: {result.Borrowing.ExpectedReturnDate:d}");
    }
}