using System;
using System.Threading.Tasks;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Xunit;

namespace EquipmentBorrowing.Tests;

public class BorrowEquipmentServiceTests
{
    private static BorrowEquipmentService BuildService()
    {
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

        return new BorrowEquipmentService(students, equipment, borrowings, maxActiveBorrowingsPerStudent: 2);
    }

    [Fact]
    public async Task Borrow_Succeeds_WhenAllRulesAreSatisfied()
    {
        var service = BuildService();

        var result = await service.BorrowAsync(studentId: 1, equipmentId: 101, expectedReturnDate: DateTime.Now.AddDays(7));

        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
        Assert.Equal(BorrowingStatus.Active, result.Borrowing!.Status);
    }

    [Fact]
    public async Task Borrow_Fails_WhenEquipmentIsUnavailable()
    {
        var service = BuildService();

        var result = await service.BorrowAsync(studentId: 1, equipmentId: 102, expectedReturnDate: DateTime.Now.AddDays(7));

        Assert.False(result.Success);
        Assert.Equal("Equipment is not currently available.", result.Message);
    }

    [Fact]
    public async Task Borrow_Fails_WhenStudentIsNotAllowedToBorrow()
    {
        var service = BuildService();

        var result = await service.BorrowAsync(studentId: 2, equipmentId: 101, expectedReturnDate: DateTime.Now.AddDays(7));

        Assert.False(result.Success);
        Assert.Equal("Student is not currently allowed to borrow equipment.", result.Message);
    }

    [Fact]
    public async Task Borrow_Fails_WhenStudentReachesMaxActiveBorrowings()
    {
        var students = new InMemoryStudentRepository(new[] { new Student(1, "Juan Dela Cruz") });
        var equipmentRepo = new InMemoryEquipmentRepository(new[]
        {
            new Equipment(101, "Multimeter"),
            new Equipment(102, "Breadboard"),
            new Equipment(103, "Function Generator")
        });
        var borrowingRepo = new InMemoryBorrowingRepository();
        var service = new BorrowEquipmentService(students, equipmentRepo, borrowingRepo, maxActiveBorrowingsPerStudent: 2);

        await service.BorrowAsync(1, 101, DateTime.Now.AddDays(7));
        await service.BorrowAsync(1, 102, DateTime.Now.AddDays(7));
        var result = await service.BorrowAsync(1, 103, DateTime.Now.AddDays(7));

        Assert.False(result.Success);
        Assert.Equal("Student has reached the maximum number of active borrowings.", result.Message);
    }
}