-- Add Status column to Semesters table
ALTER TABLE Semesters ADD COLUMN Status VARCHAR(10) NOT NULL DEFAULT 'Upcoming';

-- Migrate existing data:
-- 1. Active semester
UPDATE Semesters SET Status = 'Active' WHERE IsActive = 1;

-- 2. Ended semesters (has archived teams or archived whitelists)
UPDATE Semesters s
SET Status = 'Ended'
WHERE IsActive = 0
  AND (
    EXISTS (SELECT 1 FROM archived_teams at WHERE at.SemesterId = s.SemesterId)
    OR EXISTS (SELECT 1 FROM archived_whitelists aw WHERE aw.SemesterId = s.SemesterId)
  );

-- 3. Remaining inactive semesters stay 'Upcoming' (default)

-- Drop the obsolete IsActive column
ALTER TABLE Semesters DROP COLUMN IsActive;
