-- V48__scope_unique_indexes_by_campus.sql

-- 1. Update semesters unique index to include CampusId
-- First drop existing unique index
ALTER TABLE semesters DROP INDEX UQ_Semesters_SemesterCode;
-- Create new composite unique index
CREATE UNIQUE INDEX UQ_Semesters_SemesterCode_Campus ON semesters (SemesterCode, CampusId);

-- 2. Update teams unique index to include CampusId
-- First drop existing unique index
ALTER TABLE teams DROP INDEX TeamCode;
-- Create new composite unique index
CREATE UNIQUE INDEX UQ_Teams_TeamCode_Campus ON teams (TeamCode, CampusId);
