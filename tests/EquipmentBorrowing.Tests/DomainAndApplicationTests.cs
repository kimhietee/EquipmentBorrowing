using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

namespace EquipmentBorrowing.Tests;

public class DomainAndApplicationTests
{
    private static DateTime Tomorrow => DateTime.Today.AddDays(1);

    [Fact]
    public async Task SingleBorrow_RejectsSameDayOrPastReturnDate()
    {
        var students = new InMemoryStudentRepository(new[] { new Student(1, "2026-0001", "Ana Reyes") });
        var equipment = new InMemoryEquipmentRepository(new[] { new Equipment(101, "EQ-101", "Laptop 01") });
        var borrowings = new InMemoryBorrowingRepository(students, equipment);
        var service = new BorrowEquipmentService(students, equipment, borrowings, new InMemoryUnitOfWork());

        var sameDay = await service.BorrowAsync(1, 101, DateTime.Now);
        var missing = await service.BorrowAsync(1, 101, (DateTime?)null);

        Assert.False(sameDay.Success);
        Assert.False(missing.Success);
        Assert.True((await equipment.GetByIdAsync(101))!.IsAvailable);
    }

    [Fact]
    public async Task MultiCheckout_SucceedsAtExactLimit_AndRejectsInvalidCases()
    {
        var students = new InMemoryStudentRepository(new[]
        {
            new Student(1, "2026-0001", "Ana Reyes", isAllowedToBorrow: true),
            new Student(2, "2026-0002", "Maria Santos", isAllowedToBorrow: false)
        });
        var equipment = new InMemoryEquipmentRepository(new[]
        {
            new Equipment(101, "EQ-101", "Item 1"),
            new Equipment(102, "EQ-102", "Item 2"),
            new Equipment(103, "EQ-103", "Item 3"),
            new Equipment(104, "EQ-104", "Item 4")
        });
        var borrowings = new InMemoryBorrowingRepository(students, equipment);
        var service = new BorrowEquipmentService(students, equipment, borrowings, new InMemoryUnitOfWork(), maxActiveBorrowingsPerStudent: 3);

        // Fewer than 2 items -> rejected
        var oneItem = await service.CheckoutMultipleAsync(1, new[] { 101 }, Tomorrow);
        Assert.False(oneItem.Success);

        // Duplicate items -> rejected
        var duplicate = await service.CheckoutMultipleAsync(1, new[] { 101, 101 }, Tomorrow);
        Assert.False(duplicate.Success);

        // Ineligible student -> rejected
        var ineligible = await service.CheckoutMultipleAsync(2, new[] { 101, 102 }, Tomorrow);
        Assert.False(ineligible.Success);

        // Borrow 1 single item first (active = 1)
        Assert.True((await service.BorrowAsync(1, 101, Tomorrow)).Success);

        // Multi-checkout 2 items (1 + 2 = 3, exactly at limit) -> succeeds
        var atLimit = await service.CheckoutMultipleAsync(1, new[] { 102, 103 }, Tomorrow);
        Assert.True(atLimit.Success);
        Assert.Equal(2, atLimit.Data!.Count);

        // Any further borrow -> over limit
        var overLimit = await service.BorrowAsync(1, 104, Tomorrow);
        Assert.False(overLimit.Success);
    }

    [Fact]
    public async Task Return_AllowsEarlyReturn_AndRejectsRepeatReturn()
    {
        var students = new InMemoryStudentRepository(new[] { new Student(1, "2026-0001", "Ana Reyes") });
        var equipment = new InMemoryEquipmentRepository(new[] { new Equipment(101, "EQ-101", "Laptop 01") });
        var borrowings = new InMemoryBorrowingRepository(students, equipment);
        var uow = new InMemoryUnitOfWork();

        var borrowService = new BorrowEquipmentService(students, equipment, borrowings, uow);
        var returnService = new ReturnEquipmentService(borrowings, equipment, uow);

        var borrowed = await borrowService.BorrowAsync(1, 101, DateTime.Today.AddDays(10));
        Assert.True(borrowed.Success);

        var returned = await returnService.ReturnAsync(borrowed.Borrowing!.Id);
        Assert.True(returned.Success);
        Assert.Equal(BorrowingStatus.Returned, returned.Borrowing!.Status);
        Assert.NotNull(returned.Borrowing.ReturnedAt);
        Assert.True((await equipment.GetByIdAsync(101))!.IsAvailable);

        var repeat = await returnService.ReturnAsync(borrowed.Borrowing.Id);
        Assert.False(repeat.Success);
    }

    [Fact]
    public async Task StudentAndEquipmentManagement_EnforcesNormalizationAndClearsOptionals()
    {
        var students = new InMemoryStudentRepository();
        var equipment = new InMemoryEquipmentRepository();
        var uow = new InMemoryUnitOfWork();

        var studentSvc = new StudentManagementService(students, uow);
        var equipSvc = new EquipmentManagementService(equipment, uow);

        var s1 = await studentSvc.RegisterStudentAsync("  2026-ab01 ", " Ana Reyes ", " 0917-1234 ");
        Assert.True(s1.Success);
        Assert.True(s1.Data!.IsAllowedToBorrow);

        // Case/whitespace duplicate rejected
        var sDup = await studentSvc.RegisterStudentAsync("2026-AB01", "Other", null);
        Assert.False(sDup.Success);

        // Clear optional contact
        var sUpdated = await studentSvc.UpdateStudentAsync(s1.Data.Id, "2026-AB01", "Ana M. Reyes", "   ");
        Assert.True(sUpdated.Success);
        Assert.Null(sUpdated.Data!.Contact);

        // Eligibility repeated state
        var alreadyPermitted = await studentSvc.SetBorrowingEligibilityAsync(s1.Data.Id, true);
        Assert.True(alreadyPermitted.Success);
        Assert.Contains("already permitted", alreadyPermitted.Message, StringComparison.OrdinalIgnoreCase);

        var e1 = await equipSvc.RegisterEquipmentAsync(" eq-900 ", " Scope ", " 100MHz ");
        Assert.True(e1.Success);
        Assert.True(e1.Data!.IsAvailable);

        var eDup = await equipSvc.RegisterEquipmentAsync("EQ-900", "Another", null);
        Assert.False(eDup.Success);

        var eUpdated = await equipSvc.UpdateEquipmentAsync(e1.Data.Id, "EQ-900", "Oscilloscope", "");
        Assert.True(eUpdated.Success);
        Assert.Null(eUpdated.Data!.Description);
        Assert.True(eUpdated.Data.IsAvailable);
    }

    [Fact]
    public async Task CommitConnectionLoss_ReturnsUnknownOutcome_WithoutCallingRollback()
    {
        var students = new InMemoryStudentRepository(new[] { new Student(1, "2026-0001", "Ana Reyes") });
        var equipment = new InMemoryEquipmentRepository(new[] { new Equipment(101, "EQ-101", "Laptop 01") });
        var borrowings = new InMemoryBorrowingRepository(students, equipment);
        var fakeUow = new CommitTimeoutUnitOfWork();

        var service = new BorrowEquipmentService(students, equipment, borrowings, fakeUow);
        var result = await service.BorrowAsync(1, 101, Tomorrow);

        Assert.False(result.Success);
        Assert.True(result.IsUnknownOutcome);
        Assert.Equal(OperationStatus.UnknownOutcome, result.Status);
        Assert.False(fakeUow.RollbackCalled);
        Assert.Contains("unknown", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CommitTimeoutUnitOfWork : IUnitOfWork
    {
        public bool RollbackCalled { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => throw new TimeoutException("Simulated connection drop during commit.");

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalled = true;
            return Task.CompletedTask;
        }
    }
}
