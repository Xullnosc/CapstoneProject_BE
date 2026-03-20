-- Migration V29: Add IsSpecial flag to teams
-- This allows HOD to flag special teams to bypass the 4-member thesis proposal rule.

ALTER TABLE `teams`
ADD COLUMN `IsSpecial` TINYINT(1) NOT NULL DEFAULT 0;
