using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class BorrowEquipmentServiceTests
{
    private static BorrowEquipmentService BuildService()
    {
        var students = new InMemoryStudentRepository(new[]
        {
            new Student(1, "2026-0001", "Juan Dela Cruz", true),
            new Student(2, "2026-0002", "Maria Santos", false)
        });
        var equipment = new InMemoryEquipmentRepository(new[]
        {
            new Equipment(101, "Digital Multimeter", "Instrument", true),
            new Equipment(102, "Oscilloscope", "Instrument", false)
        });
        return new BorrowEquipmentService(students, equipment,
            new InMemoryBorrowingRepository(), new InMemoryUnitOfWork(), 2);
    }

    [Fact]
    public async Task Borrow_Succeeds_WhenAllRulesAreSatisfied()
    {
        var result = await BuildService().BorrowAsync(1, 101, DateTime.Now.AddDays(7));
        Assert.True(result.Success);
        Assert.NotNull(result.Borrowing);
        Assert.Equal(BorrowingStatus.Active, result.Borrowing!.Status);
    }

    [Fact]
    public async Task Borrow_Fails_WhenEquipmentIsUnavailable()
    {
        var result = await BuildService().BorrowAsync(1, 102, DateTime.Now.AddDays(7));
        Assert.False(result.Success);
        Assert.Equal("Equipment is not currently available.", result.Message);
    }

    [Fact]
    public async Task Borrow_Fails_WhenStudentIsNotAllowedToBorrow()
    {
        var result = await BuildService().BorrowAsync(2, 101, DateTime.Now.AddDays(7));
        Assert.False(result.Success);
        Assert.Equal("Student is not currently allowed to borrow equipment.", result.Message);
    }

    [Fact]
    public async Task Borrow_Fails_WhenStudentReachesMaxActiveBorrowings()
    {
        var students = new InMemoryStudentRepository(new[] { new Student(1, "2026-0001", "Juan Dela Cruz") });
        var equipmentRepo = new InMemoryEquipmentRepository(new[]
        {
            new Equipment(101, "Multimeter", "Instrument"),
            new Equipment(102, "Breadboard", "Component"),
            new Equipment(103, "Function Generator", "Instrument")
        });
        var service = new BorrowEquipmentService(students, equipmentRepo,
            new InMemoryBorrowingRepository(), new InMemoryUnitOfWork(), 2);

        await service.BorrowAsync(1, 101, DateTime.Now.AddDays(7));
        await service.BorrowAsync(1, 102, DateTime.Now.AddDays(7));
        var result = await service.BorrowAsync(1, 103, DateTime.Now.AddDays(7));

        Assert.False(result.Success);
        Assert.Equal("Student has reached the maximum number of active borrowings.", result.Message);
    }
}