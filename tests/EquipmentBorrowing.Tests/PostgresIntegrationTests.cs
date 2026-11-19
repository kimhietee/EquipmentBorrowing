using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EquipmentBorrowing.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private string _testConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("79c0898e-6498-434e-b445-f26d621c2941")
            .Build();

        var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default for integration tests.");

        var builder = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = "equipment_borrowing_test"
        };
        _testConnectionString = builder.ConnectionString;

        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_testConnectionString))
            return;

        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }

    private EquipmentBorrowingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;
        return new EquipmentBorrowingDbContext(options);
    }

    private (BorrowEquipmentService borrowSvc, ReturnEquipmentService returnSvc, StudentManagementService studentSvc, EquipmentManagementService equipSvc, BorrowingQueryService querySvc)
        CreateServices(EquipmentBorrowingDbContext db)
    {
        var students = new EfStudentRepository(db);
        var equipment = new EfEquipmentRepository(db);
        var borrowings = new EfBorrowingRepository(db);
        var uow = new EfUnitOfWork(db);
        var classifier = new NpgsqlPersistenceExceptionClassifier();

        return (
            new BorrowEquipmentService(students, equipment, borrowings, uow, 3, classifier),
            new ReturnEquipmentService(borrowings, equipment, uow, classifier),
            new StudentManagementService(students, uow, classifier),
            new EquipmentManagementService(equipment, uow, classifier),
            new BorrowingQueryService(equipment, students, borrowings, classifier)
        );
    }

    [Fact]
    public async Task EndToEndWorkflows_PersistAcrossFreshDbContexts_AndRollbackInTransactionFailure()
    {
        int studentAId, studentBId, equip1Id, equip2Id, equip3Id;

        // 1. Register students and equipment (A12, A15)
        await using (var db = CreateDbContext())
        {
            var (_, _, studentSvc, equipSvc, _) = CreateServices(db);

            var sA = await studentSvc.RegisterStudentAsync(" 2026-1001 ", "Alice Cruz", "0917-111-2222");
            var sB = await studentSvc.RegisterStudentAsync("2026-1002", "Bob Lim", null);
            Assert.True(sA.Success);
            Assert.True(sB.Success);
            studentAId = sA.Data!.Id;
            studentBId = sB.Data!.Id;

            // Duplicate normalized number rejected (A12)
            var dupStudent = await studentSvc.RegisterStudentAsync(" 2026-1001 ", "Clone", null);
            Assert.False(dupStudent.Success);

            var e1 = await equipSvc.RegisterEquipmentAsync("lab-01", "Microscope", "Optical");
            var e2 = await equipSvc.RegisterEquipmentAsync("LAB-02", "Centrifuge", null);
            var e3 = await equipSvc.RegisterEquipmentAsync("LAB-03", "Spectrometer", "UV-Vis");
            Assert.True(e1.Success && e2.Success && e3.Success);
            equip1Id = e1.Data!.Id;
            equip2Id = e2.Data!.Id;
            equip3Id = e3.Data!.Id;

            // Duplicate normalized equipment identifier rejected (A15)
            var dupEquip = await equipSvc.RegisterEquipmentAsync(" LAB-01 ", "Duplicate", null);
            Assert.False(dupEquip.Success);
        }

        // 2. Edit student & equipment, clear optional fields, verify fresh readback (A13, A16)
        await using (var db = CreateDbContext())
        {
            var (_, _, studentSvc, equipSvc, _) = CreateServices(db);
            var upS = await studentSvc.UpdateStudentAsync(studentAId, "2026-1001", "Alice M. Cruz", "  ");
            Assert.True(upS.Success);
            Assert.Null(upS.Data!.Contact);

            var upE = await equipSvc.UpdateEquipmentAsync(equip1Id, "LAB-01", "Microscope Pro", "   ");
            Assert.True(upE.Success);
            Assert.Null(upE.Data!.Description);
        }

        // 3. Single borrow + Restrict student + return while restricted (A03, A14, A05)
        int firstBorrowingId;
        await using (var db = CreateDbContext())
        {
            var (borrowSvc, _, studentSvc, _, _) = CreateServices(db);
            var b1 = await borrowSvc.BorrowAsync(studentAId, equip1Id, DateTime.Today.AddDays(3));
            Assert.True(b1.Success);
            firstBorrowingId = b1.Borrowing!.Id;

            var restrict = await studentSvc.SetBorrowingEligibilityAsync(studentAId, false);
            Assert.True(restrict.Success);

            var blocked = await borrowSvc.BorrowAsync(studentAId, equip2Id, DateTime.Today.AddDays(3));
            Assert.False(blocked.Success);
        }

        await using (var db = CreateDbContext())
        {
            var (_, returnSvc, studentSvc, _, querySvc) = CreateServices(db);
            // Restricted student can still return existing active borrowing (A14, A05)
            var ret = await returnSvc.ReturnAsync(firstBorrowingId);
            Assert.True(ret.Success);

            // Repeat return rejected
            var repeat = await returnSvc.ReturnAsync(firstBorrowingId);
            Assert.False(repeat.Success);

            // Re-permit student
            var permit = await studentSvc.SetBorrowingEligibilityAsync(studentAId, true);
            Assert.True(permit.Success);

            // Fresh readback verifies available equipment
            var catalog = await querySvc.GetEquipmentCatalogAsync(availableOnly: true);
            Assert.Equal(3, catalog.Data!.Count);
        }

        // 4. Controlled in-transaction failure in Multi-Item Checkout leaves zero partial changes (A06)
        await using (var db = CreateDbContext())
        {
            var (borrowSvc, _, _, _, _) = CreateServices(db);
            var failedMulti = await borrowSvc.CheckoutMultipleAsync(
                studentAId,
                new[] { equip1Id, equip2Id },
                DateTime.Today.AddDays(5),
                beforeCommitHook: _ => throw new InvalidOperationException("Controlled in-transaction failure after SaveChangesAsync."));

            Assert.False(failedMulti.Success);
            Assert.Equal(OperationStatus.Failure, failedMulti.Status);
        }

        // Verify via fresh DbContext that NO active borrowings were created and both items remain available
        await using (var db = CreateDbContext())
        {
            var (_, _, _, _, querySvc) = CreateServices(db);
            var active = await querySvc.GetActiveBorrowingsAsync();
            Assert.Empty(active.Data!);

            var available = await querySvc.GetEquipmentCatalogAsync(availableOnly: true);
            Assert.Equal(3, available.Data!.Count);
        }

        // 5. Valid Multi-Item Checkout persists both records together (A06)
        await using (var db = CreateDbContext())
        {
            var (borrowSvc, _, _, _, _) = CreateServices(db);
            var okMulti = await borrowSvc.CheckoutMultipleAsync(
                studentAId,
                new[] { equip2Id, equip3Id },
                DateTime.Today.AddDays(6));

            Assert.True(okMulti.Success);
            Assert.Equal(2, okMulti.Data!.Count);
        }

        // 6. Histories (A07, A08) & Search/Filter/Sort (A09) in fresh DbContext
        await using (var db = CreateDbContext())
        {
            var (_, _, _, _, querySvc) = CreateServices(db);

            var studentAHistory = await querySvc.GetStudentHistoryAsync(studentAId);
            Assert.True(studentAHistory.Success, studentAHistory.Message);
            Assert.Equal(3, studentAHistory.Data!.Count); // 1 returned + 2 active

            var studentBHistory = await querySvc.GetStudentHistoryAsync(studentBId);
            Assert.True(studentBHistory.Success, studentBHistory.Message);
            Assert.Empty(studentBHistory.Data!);

            var equip1History = await querySvc.GetEquipmentHistoryAsync(equip1Id);
            Assert.True(equip1History.Success, equip1History.Message);
            Assert.Single(equip1History.Data!);
            Assert.Equal(BorrowingStatus.Returned, equip1History.Data![0].Status);

            var activeOnly = await querySvc.SearchBorrowingsAsync(
                new BorrowingSearchQuery("  alice ", BorrowingStatusFilter.Active, BorrowingSortField.ExpectedReturnDate, SortDirection.Ascending));
            Assert.True(activeOnly.Success, activeOnly.Message);
            Assert.Equal(2, activeOnly.Data!.Count);

            var returnedOnly = await querySvc.SearchBorrowingsAsync(
                new BorrowingSearchQuery("microscope", BorrowingStatusFilter.Returned, BorrowingSortField.DateBorrowed, SortDirection.Descending));
            Assert.True(returnedOnly.Success, returnedOnly.Message);
            Assert.Single(returnedOnly.Data!);
        }

        // 7. Database-level constraint enforcement check (A17): duplicate active borrowing rejected by PostgreSQL
        await using (var db = CreateDbContext())
        {
            db.Borrowings.Add(new Borrowing(0, studentBId, equip2Id, DateTime.Today, DateTime.Today.AddDays(2)));
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var classifier = new NpgsqlPersistenceExceptionClassifier();
            Assert.Equal(PersistenceFailureKind.UniqueConstraintViolation, classifier.Classify(ex));
        }
    }

    [Fact]
    public async Task UnreachableDatabase_ReturnsSanitizedErrorWithoutCredentials()
    {
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql("Host=localhost;Port=59999;Database=nonexistent_db;Username=postgres;Password=super_secret_password;Timeout=1")
            .Options;

        await using var db = new EquipmentBorrowingDbContext(options);
        var (_, _, _, _, querySvc) = CreateServices(db);

        var result = await querySvc.GetEquipmentCatalogAsync(false);

        Assert.False(result.Success);
        Assert.DoesNotContain("super_secret_password", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
