-- Verification Script: Check for existing data that violates the new unique constraint
-- Run this on the database before deploying the migration to identify potential issues

-- Check for duplicate (ClassroomId, DayOfWeek, TimeIn, TimeOut) combinations
-- These would violate the new unique constraint
SELECT 
    ClassroomId,
    DayOfWeek,
    TimeIn,
    TimeOut,
    COUNT(*) as DuplicateCount
FROM Schedules
GROUP BY ClassroomId, DayOfWeek, TimeIn, TimeOut
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC, ClassroomId, DayOfWeek, TimeIn;

-- If the above query returns any rows, you have data that will cause the migration to fail
-- Review the duplicate schedules and resolve them before deploying

-- Optional: Show the actual duplicate schedule records for investigation
-- Uncomment and run if the above query returns results
/*
SELECT 
    s.Id,
    s.ClassroomId,
    s.DayOfWeek,
    s.TimeIn,
    s.TimeOut,
    s.SubjectId,
    s.SectionId,
    s.InstructorId,
    s.CreatedAt
FROM Schedules s
WHERE EXISTS (
    SELECT 1
    FROM Schedules s2
    WHERE s2.ClassroomId = s.ClassroomId
      AND s2.DayOfWeek = s.DayOfWeek
      AND s2.TimeIn = s.TimeIn
      AND s2.TimeOut = s.TimeOut
      AND s2.Id <> s.Id
)
ORDER BY s.ClassroomId, s.DayOfWeek, s.TimeIn;
*/

-- Check current index state
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.Schedules')
  AND i.name LIKE '%Schedule%'
GROUP BY i.name, i.type_desc, i.is_unique
ORDER BY i.name;
