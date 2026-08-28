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
