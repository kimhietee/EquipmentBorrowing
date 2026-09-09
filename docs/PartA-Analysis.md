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
