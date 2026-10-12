# Part A – Analyze the System Before Coding

## A. Actors

| Actor | What they expect from the system |
|---|---|
| **Student** | Wants to check whether equipment is available, borrow it if eligible, and get a clear reason if a request is rejected. |
| **Laboratory Staff** | Operates the system when a student checks equipment out or in; expects the system to enforce the borrowing rules automatically. |

## B. Use Cases

### Use Case 1

| Item | Description |
|---|---|
| Use Case | Borrow Equipment |
| Primary Actor | Student |
| Preconditions | Student is registered in the system; the requested equipment exists in the catalog |
| Main Action | Student submits a request to borrow a specific piece of equipment |
| Expected Result | A new Borrowing record is created with status `Active`; the equipment becomes unavailable |
| Possible Failure | Student is not allowed to borrow, equipment does not exist, equipment is already borrowed, or student has reached the maximum number of active borrowings |

### Use Case 2

| Item | Description |
|---|---|
| Use Case | Return Equipment |
| Primary Actor | Student (equipment physically handed back to Laboratory Staff) |
| Preconditions | An `Active` borrowing record exists linking this student to this equipment |
| Main Action | The system marks the matching borrowing record as returned |
| Expected Result | Borrowing status changes to `Returned`; equipment becomes available again |
| Possible Failure | No matching active borrowing exists for that student/equipment pair |

### Use Case 3

| Item | Description |
|---|---|
| Use Case | Find Available Equipment |
| Primary Actor | Student |
| Preconditions | None |
| Main Action | Student browses or searches the equipment catalog |
| Expected Result | System returns the list of equipment currently marked as available |
| Possible Failure | No equipment matches the search, or the catalog is empty |

## C. Domain Concepts

### Student
1. **Must contain:** unique Id, Name, whether they are currently allowed to borrow (`IsAllowedToBorrow`).
2. **Rules/state it owns:** its own eligibility flag.
3. **Not its responsibility:** counting how many items the student currently has borrowed (that belongs to the Borrowing records collectively, not to Student), and deciding whether a specific borrow request should be approved (that's an Application-layer decision).

### Equipment
1. **Must contain:** unique Id, Name, availability flag (`IsAvailable`).
2. **Rules/state it owns:** only itself can flip available → unavailable and back (`MarkAsBorrowed()` / `MarkAsAvailable()`).
3. **Not its responsibility:** knowing *who* borrowed it or *when* — that belongs to the Borrowing record.

### Borrowing
1. **Must contain:** Id, StudentId, EquipmentId, DateBorrowed, ExpectedReturnDate, Status.
2. **Rules/state it owns:** its own status transition (`MarkAsReturned()`), refuses to be returned twice.
3. **Not its responsibility:** deciding *whether* a borrow should be allowed in the first place — that requires checking Student, Equipment, and other Borrowing records together, which is why that logic lives in `BorrowEquipmentService`, not inside Borrowing itself.

---

# Laboratory Activity 2 — Avalonia UI and MVVM

## 1. Desktop Project

`EquipmentBorrowing.Desktop` is the presentation layer added in this activity. It is responsible for displaying information, collecting user input, and invoking existing application operations through ViewModels. It references `EquipmentBorrowing.Application` (to call `BorrowEquipmentService` and `ReturnEquipmentService`) and `EquipmentBorrowing.Infrastructure` (to construct the concrete in-memory repositories in its composition root, `App.axaml.cs`). It does not reference `EquipmentBorrowing.Domain` directly — Domain types are used transitively through Application.

## 2. Updated Architecture

```text
Avalonia View (EquipmentView / BorrowingsView)
        │
        │ Binding / Command
        ▼
ViewModel (EquipmentViewModel / BorrowingsViewModel)
        │
        │ Application Operation
        ▼
Application Service (BorrowEquipmentService / ReturnEquipmentService)
        │
        ├──────────► Domain (Student, Equipment, Borrowing)
        │
        ▼
Repository Interface (IStudentRepository, IEquipmentRepository, IBorrowingRepository)
        ▲
        │
Infrastructure Implementation (InMemory...Repository)
```

## 3. Borrow Equipment Flow

The user selects a student and a piece of equipment on the Equipment page, picks an expected return date, and presses "Borrow Equipment." This triggers `EquipmentViewModel.BorrowCommand`, which calls `BorrowEquipmentService.BorrowAsync(...)` with the selected IDs and date. The service performs all validation (student exists and is allowed, equipment exists and is available, max active borrowings not reached) using the repositories, then creates a `Borrowing` record and marks the equipment unavailable. The result (success or a specific failure message) is returned to the ViewModel, which displays it as `StatusMessage`, and reloads the equipment list on success so the UI reflects the new availability state.

## 4. Return Equipment Flow

On the Active Borrowings page, the user selects an active borrowing and presses "Return Equipment." This triggers `BorrowingsViewModel.ReturnCommand`, which calls `ReturnEquipmentService.ReturnAsync(borrowingId)`. The service looks up the borrowing, confirms it has not already been returned, marks it as `Returned`, and marks the associated equipment available again. The result is shown via `StatusMessage`, and the active borrowings list reloads so the returned item disappears from it.

## 5. Architectural Reflection

**Why should the View not call a repository directly?**
The View's job is layout and presentation only. If it called a repository directly, business rules (like checking borrowing limits) would have no single enforced location, and the View would need to know about Infrastructure concerns it has no business knowing about.

**Why should business rules not be implemented in the ViewModel?**
The ViewModel's job is to collect input and invoke an operation, not to decide whether that operation is valid. Keeping rules in the Application/Domain layers means the same rule is enforced identically no matter which UI (or future UI) calls it, and the rule can be tested without any UI involved at all, as the existing test suite shows.

**What is the responsibility of the ViewModel?**
To hold presentation state (selected items, status messages, observable collections for binding) and to translate user actions (commands) into calls to existing application services, then reflect the result back to the View.

**Why can the existing Application layer work without knowing that Avalonia is being used?**
Because `BorrowEquipmentService` and `ReturnEquipmentService` depend only on repository interfaces and Domain types, never on anything UI-related. Avalonia is layered entirely on top, in a separate project, and the dependency direction only ever points from Desktop toward Application, never the reverse.

**What advantage is gained from registering dependencies in one composition point?**
All object construction and wiring happens in exactly one place (`App.axaml.cs`), so the actual business logic classes never need to know how their dependencies are created. This also means switching to a real DI container later, or swapping in-memory repositories for database-backed ones, only requires changing that one composition point.

**If the in-memory repository were replaced by SQLite later, which parts of the current interface should remain largely unchanged?**
Everything except the three `InMemory...Repository` classes. The Views, ViewModels, Application services, Domain models, and repository interfaces would all remain exactly as they are, since they were never written against any specific storage mechanism.
