-- ITSD 81 Laboratory Activity 3 - Campus Equipment Borrowing System
-- Demonstration queries for the SQLite schema created by the EF Core migration.
-- Table names: Students, Equipment, Borrowings.
-- Note: SQLite stores booleans as 0/1 and the Status enum as text ('Active' / 'Returned').

-- 1. BASIC RETRIEVAL: all equipment
SELECT *
FROM Equipment;

-- 2. FILTERING: only currently available equipment
SELECT Id, Name, Type
FROM Equipment
WHERE IsAvailable = 1
ORDER BY Name;

-- 3. JOIN: active borrowings with student and equipment information
SELECT s.Name              AS Student,
       e.Name              AS Equipment,
       date(b.DateBorrowed)       AS Borrowed,
       date(b.ExpectedReturnDate) AS Due
FROM Borrowings AS b
INNER JOIN Students  AS s ON s.Id = b.StudentId
INNER JOIN Equipment AS e ON e.Id = b.EquipmentId
WHERE b.Status = 'Active'
ORDER BY b.ExpectedReturnDate;

-- 4. AGGREGATES
-- 4a. Number of active borrowings per student (students with none show 0)
SELECT s.Name, COUNT(b.Id) AS ActiveBorrowings
FROM Students AS s
LEFT JOIN Borrowings AS b
       ON b.StudentId = s.Id AND b.Status = 'Active'
GROUP BY s.Id, s.Name
ORDER BY ActiveBorrowings DESC, s.Name;

-- 4b. Number of equipment records by type
SELECT Type, COUNT(*) AS Items
FROM Equipment
GROUP BY Type
ORDER BY Items DESC;

-- 5. UPDATE: change an appropriate field
-- Example: block a student from borrowing (e.g. unpaid fines).
UPDATE Students
SET IsAllowedToBorrow = 0
WHERE StudentNumber = '2026-0002';
-- Undo it:
-- UPDATE Students SET IsAllowedToBorrow = 1 WHERE StudentNumber = '2026-0002';
