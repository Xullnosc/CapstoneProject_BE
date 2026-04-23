-- Migration V65: Fix whitelist uniqueness to allow returning students across semesters
--
-- Root cause: the old UNIQUE (Email) constraint was global across all semesters.
-- Attempting to import a student from a previous semester into a new one triggered a
-- DbUpdateException because ReconcileSemesterAsync correctly inserts a new row per semester.
--
-- Fix: replace with UNIQUE (Email, SemesterId) so one row per student per semester is
-- allowed while history from previous semesters is preserved.
--
-- IMPORTANT — NULL SemesterId rows (global lecturer/HOD whitelist entries):
-- MySQL treats NULL as DISTINCT in UNIQUE indexes, so (email, NULL) pairs are NOT
-- protected by this constraint. Uniqueness for those rows is enforced at the
-- application layer (WhitelistService / SemesterService).
--
-- Safety: ADD the new constraint BEFORE dropping the old one.
-- If the ADD fails (unexpected duplicates), the original constraint remains intact.
--
-- Pre-flight check (run manually before applying):
--   SELECT Email, SemesterId, COUNT(*) c
--   FROM whitelist
--   GROUP BY Email, SemesterId
--   HAVING c > 1;
-- Must return 0 rows.
--
-- Rollback (keep this script for emergencies):
--   ALTER TABLE whitelist ADD CONSTRAINT UQ__Whitelis__A9D10534BDF4FDF3 UNIQUE (Email);
--   ALTER TABLE whitelist DROP INDEX UQ_Whitelist_Email_Semester;

-- Step 1: Add the new composite unique constraint first
ALTER TABLE whitelist
    ADD CONSTRAINT UQ_Whitelist_Email_Semester UNIQUE (Email, SemesterId);

-- Step 2: Remove the old Email-only unique constraint only after the new one is in place
ALTER TABLE whitelist
    DROP INDEX UQ__Whitelis__A9D10534BDF4FDF3;
