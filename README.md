# Equipment Borrowing System - Laboratory Activity 1

## 1. Solution Structure

- **Domain** - the core concepts of the problem (`Student`, `Equipment`,
  `Borrowing`, `BorrowingStatus`). No dependencies on any other project.
- **Application** - the use case (`BorrowEquipmentService`) and the
  repository interfaces it depends on (`IStudentRepository`,
  `IEquipmentRepository`, `IBorrowingRepository`). Depends only on Domain.
- **Infrastructure** - in-memory `List<T>` based implementations of those
  interfaces. Depends on both Domain and Application.
- **Tests** - automated tests exercising `BorrowEquipmentService` through
  the in-memory repositories, proving one success case and three failure
  cases.

## 2. Dependency Direction

```text
Tests -> Infrastructure -> Application -> Domain
                 \_______________________/
```

Infrastructure depends on Application (to implement its interfaces) and on
Domain. Application depends only on Domain. Domain depends on nothing.
Dependencies always point inward, toward Domain.

## 3. Use Case Mapping

```text
Actor: Student
Use Case: Borrow Equipment
Application Service: BorrowEquipmentService
Domain Objects Used: Student, Equipment, Borrowing, BorrowingStatus
Repository Interfaces Used: IStudentRepository, IEquipmentRepository, IBorrowingRepository
Infrastructure Implementations Used: InMemoryStudentRepository, InMemoryEquipmentRepository, InMemoryBorrowingRepository
```

## 4. Reflection

**1. Why should the application service depend on a repository interface
instead of directly depending on a database implementation?**
So the business logic only cares about *what* it needs (find a student,
save a borrowing), not *how* that happens. The storage technology can
change - or be replaced with an in-memory fake for testing, as this
activity does - without changing any business logic.

**2. Which parts of your current solution could remain unchanged if SQLite
were added later?**
All of Domain and Application. Only Infrastructure would change: a new
`SqliteEquipmentRepository` etc. would be added, implementing the same
interfaces the in-memory versions implement today.

**3. Which project would eventually contain Avalonia Views?**
A new project (e.g. `EquipmentBorrowing.Presentation`) that references
Application, since the UI's job is to call application services and
display their results.

**4. Should an Avalonia button directly execute database queries? Why or
why not?**
No. That would collapse the layering built here - the UI would become
tightly coupled to one database technology, business rules would end up
scattered inside click handlers, and nothing could be unit tested without a
real database running.

**5. What part of your implementation represents the actual business
operation requested by the actor?**
`BorrowEquipmentService.BorrowAsync()` - the only place where the "can this
borrow happen?" decision is made.

---

# Laboratory Activity 3 - From In-Memory Data to Persistent Storage

## 1. Relational Database Design

![Database diagram](docs/database-diagram.png)

| Table | Key columns | Constraints |
|---|---|---|
| **Students** | `Id` (PK) | `StudentNumber` unique, max 20; `Name` required, max 100; `IsAllowedToBorrow` required |
| **Equipment** | `Id` (PK) | `Name` unique, max 100; `Type` required, max 50, indexed; `IsAvailable` required |
| **Borrowings** | `Id` (PK), `StudentId` (FK), `EquipmentId` (FK) | `DateBorrowed`, `ExpectedReturnDate` required; `ReturnedAt` optional (NULL until returned); `Status` stored as text (`Active`/`Returned`) |

**Relationships:** one Student has many Borrowings; one Equipment item appears in many Borrowings over time; each Borrowing belongs to exactly one Student and one Equipment item. Foreign keys use `ON DELETE RESTRICT`, so a student or item with history cannot be deleted.

**Indexes:** unique `Students.StudentNumber`; unique `Equipment.Name`; `Equipment.Type`; composite `Borrowings(StudentId, Status)` for the "active borrowings per student" rule; and a filtered unique index on `Borrowings.EquipmentId WHERE Status = 'Active'`, so an item can never have two active loans at once. Borrowings store only IDs, never copies of student or equipment data.

## 2. SQLite and EF Core

Added to `EquipmentBorrowing.Infrastructure`: `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design`. The Desktop project references `Microsoft.Extensions.DependencyInjection`. `dotnet-ef` was installed as a global tool. All EF Core code stays in Infrastructure; Domain and Application have no EF references. The database file is `%LOCALAPPDATA%\EquipmentBorrowing\equipmentborrowing.db`, so it does not depend on the working directory.

## 3. DbContext

`EquipmentBorrowingDbContext` represents one session with the database. It exposes `Students`, `Equipment`, and `Borrowings`, loads the entity mappings from the `IEntityTypeConfiguration<T>` classes in `Persistence/Configurations`, tracks changes to loaded entities, and translates `SaveChangesAsync` into SQL inside a transaction. Only Infrastructure classes use it; Views and ViewModels never see it.

## 4. Repository Transition

```text
Lab 2:  IEquipmentRepository -> InMemoryEquipmentRepository (List<T>)
Lab 3:  IEquipmentRepository -> EfEquipmentRepository -> EquipmentBorrowingDbContext -> SQLite
```

Interfaces gained only what Lab 3 needed (`GetAvailableAsync`, `GetActiveBorrowingDetailsAsync`) plus `IUnitOfWork`. `IUnitOfWork.SaveChangesAsync` lets the services save the equipment change and the new borrowing together in one transaction. The services still depend only on interfaces.

## 5. Migration Process

```powershell
dotnet tool update --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/EquipmentBorrowing.Infrastructure --output-dir Migrations
dotnet ef database update --project src/EquipmentBorrowing.Infrastructure
```

The app also calls `Database.MigrateAsync()` at startup, which applies only pending migrations and never drops or recreates the database. Seed data (3 students, 6 equipment items, 1 active borrowing) is inserted only when the Students table is empty.

## 6. Generated SQL

> PASTE YOUR REAL OUTPUT HERE (see instructions in the hand-off notes). Keep the LINQ blocks; replace the SQL with what EF Core actually logged.

**Query A - Active borrowings with student and equipment (`EfBorrowingRepository.GetActiveBorrowingDetailsAsync`)**

```csharp
from b in _db.Borrowings.AsNoTracking()
join s in _db.Students on b.StudentId equals s.Id
join e in _db.Equipment on b.EquipmentId equals e.Id
where b.Status == BorrowingStatus.Active
orderby b.ExpectedReturnDate
select new ActiveBorrowingDetails(b.Id, s.StudentNumber, s.Name, e.Name, b.DateBorrowed, b.ExpectedReturnDate);
```
```sql
-- paste logged SQL here (expected: SELECT ... FROM "Borrowings" AS "b"
--   INNER JOIN "Students" AS "s" ... INNER JOIN "Equipment" AS "e" ...
--   WHERE "b"."Status" = 'Active' ORDER BY "b"."ExpectedReturnDate")
```
*Explanation:* EF Core turns the two LINQ joins into SQL INNER JOINs on the foreign keys, filters on the text-converted enum, and returns only the columns the screen needs, so no full entities are loaded.

**Query B - Available equipment (`EfEquipmentRepository.GetAvailableAsync`)**

```csharp
_db.Equipment.AsNoTracking().Where(e => e.IsAvailable).OrderBy(e => e.Name).ToListAsync();
```
```sql
-- paste logged SQL here (expected: SELECT ... FROM "Equipment" AS "e"
--   WHERE "e"."IsAvailable" ORDER BY "e"."Name")
```
*Explanation:* the C# predicate becomes a SQL WHERE clause evaluated by SQLite, so only matching rows leave the database.

**Query C - Active borrowings for a student (`CountActiveBorrowingsByStudentAsync`)** is an `COUNT(*)` query filtered by `StudentId` and `Status`, served by the composite index.

## 7. Tracking and No-Tracking

| Operation | Behavior | Why |
|---|---|---|
| Equipment list, student list, available equipment, active-borrowing details | `AsNoTracking()` | Display only. No change tracking is needed, so EF skips the snapshot overhead. |
| `GetByIdAsync` for Equipment, Student, Borrowing | Tracked | Borrow and Return change `IsAvailable`, `Status`, and `ReturnedAt`. EF detects the changes and writes them on `SaveChangesAsync`. |

## 8. Persistence Demonstration

1. Started the app; Equipment list loaded from SQLite. 2. Borrowed an item and saw it in Active Borrowings. 3. Closed the app completely and restarted: the borrowing was still listed and the item still unavailable. 4. Returned it, closed and restarted: the borrowing was gone from Active Borrowings and the item was available again. (Screenshots in `docs/`.)

## 9. Architectural Reflection

1. **Why no rewrite?** The Views, ViewModels, and services depend on repository interfaces. Only the implementations behind them changed, plus the registrations in the composition root.
2. **Why shouldn't the ViewModel use DbContext?** It would tie presentation code to EF Core and SQLite, mix persistence into UI logic, and make the business rules and ViewModels untestable without a database.
3. **What does the repository implementation do now?** It translates application requests ("get available equipment", "add a borrowing") into EF Core queries and commands against the DbContext.
4. **Purpose of a migration?** It is a versioned, repeatable description of schema changes, so the database can be built from the repository history instead of being created by hand.
5. **Why foreign keys?** They guarantee every borrowing points to a real student and a real equipment item, and they prevent deleting records that still have borrowing history.
6. **Why AsNoTracking for read-only queries?** EF does not need to track entities it will never save, which saves memory and time and avoids stale tracked copies.
7. **If SQLite were replaced?** Only Infrastructure changes: the provider package, the connection string, and possibly a regenerated migration. Domain, Application, ViewModels, and Views stay the same.
