-- SQL Script to fix incorrect enrollment types
-- Updates StudentEnrollment records where EnrollmentType is 'Regular' but the student's
-- primary section (Student.SectionId) differs from the enrollment section (StudentEnrollment.SectionId)
-- These should be marked as 'Irregular' instead of 'Regular'

-- First, review the records that will be updated (dry run)
SELECT 
    se.Id AS EnrollmentId,
    s.Id AS StudentId,
    s.Firstname,
    s.Lastname,
    s.SectionId AS StudentPrimarySectionId,
    se.SectionId AS EnrollmentSectionId,
    se.SubjectId,
    se.EnrollmentType AS CurrentEnrollmentType,
    'Irregular' AS CorrectEnrollmentType,
    se.IsActive,
    se.EnrolledAt
FROM StudentEnrollments se
INNER JOIN Students s ON se.StudentId = s.Id
WHERE se.EnrollmentType = 'Regular'
  AND s.SectionId != se.SectionId
  AND s.IsDeleted = 0
  AND se.IsActive = 1;

-- Execute the update to fix incorrect enrollment types
UPDATE StudentEnrollments
SET EnrollmentType = 'Irregular',
    UpdatedAt = GETDATE()
WHERE EnrollmentType = 'Regular'
  AND StudentId IN (
    SELECT s.Id 
    FROM Students s 
    WHERE s.SectionId != StudentEnrollments.SectionId
      AND s.IsDeleted = 0
  )
  AND IsActive = 1;

-- Verify the update was successful
SELECT 
    se.Id AS EnrollmentId,
    s.Id AS StudentId,
    s.Firstname,
    s.Lastname,
    s.SectionId AS StudentPrimarySectionId,
    se.SectionId AS EnrollmentSectionId,
    se.SubjectId,
    se.EnrollmentType AS UpdatedEnrollmentType,
    se.IsActive,
    se.UpdatedAt
FROM StudentEnrollments se
INNER JOIN Students s ON se.StudentId = s.Id
WHERE se.EnrollmentType = 'Irregular'
  AND s.SectionId != se.SectionId
  AND s.IsDeleted = 0
  AND se.IsActive = 1
  AND se.UpdatedAt >= DATEADD(minute, -5, GETDATE()); -- Records updated in last 5 minutes

-- Optional: If you want to see the count of affected records
SELECT COUNT(*) AS TotalRecordsUpdated
FROM StudentEnrollments
WHERE EnrollmentType = 'Irregular'
  AND StudentId IN (
    SELECT s.Id 
    FROM Students s 
    WHERE s.SectionId != StudentEnrollments.SectionId
      AND s.IsDeleted = 0
  )
  AND IsActive = 1
  AND UpdatedAt >= DATEADD(minute, -5, GETDATE());
