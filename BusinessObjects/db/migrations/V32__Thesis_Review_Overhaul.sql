-- V32: Thesis Review Overhaul (Horizontal Structure)
-- Consolidates overhaul from previous attempts.
-- Moves IsReviewer flag to lecturer table.
-- Drops old structures and creates horizontal slots for 2 reviewers.

-- 1. Drop old structures (Ensure clean state for horizontal structure)
DROP TABLE IF EXISTS `thesis_reviews`;

-- 2. Create refined thesis_reviews table (Horizontal Structure)
CREATE TABLE `thesis_reviews` (
    `ThesisId` CHAR(36) NOT NULL,
    `Reviewer1Id` INT NULL,
    `Reviewer2Id` INT NULL,
    `Reviewer1Decision` ENUM('Pass', 'Fail') NULL,
    `Reviewer2Decision` ENUM('Pass', 'Fail') NULL,
    `Reviewer1Comment` TEXT NULL,
    `Reviewer2Comment` TEXT NULL,
    `Reviewer1FileUrl` VARCHAR(500) NULL,
    `Reviewer2FileUrl` VARCHAR(500) NULL,
    `Reviewer1Date` DATETIME NULL,
    `Reviewer2Date` DATETIME NULL,
    PRIMARY KEY (`ThesisId`),
    CONSTRAINT `FK_Reviews_Thesis_Identity` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
    CONSTRAINT `FK_Reviews_Reviewer1` FOREIGN KEY (`Reviewer1Id`) REFERENCES `users` (`UserID`) ON DELETE SET NULL,
    CONSTRAINT `FK_Reviews_Reviewer2` FOREIGN KEY (`Reviewer2Id`) REFERENCES `users` (`UserID`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. Update Lecturer table
-- Adding BIT column if not exists. If V33 was run partially, we handle it.
ALTER TABLE `lecturers`
ADD COLUMN `IsReviewer` BIT NOT NULL DEFAULT 0;

-- 4. Clean up Whitelist table
ALTER TABLE `whitelist`
DROP COLUMN `IsReviewer`;

-- 5. Trigger to ensure IsActive=0 => IsReviewer=0
DELIMITER //
CREATE TRIGGER `trg_lecturer_deactivate` BEFORE UPDATE ON `lecturers`
FOR EACH ROW
BEGIN
    IF NEW.IsActive = 0 THEN
        SET NEW.IsReviewer = 0;
    END IF;
END //
DELIMITER ;

-- 6. Rename Note to Comment in thesis_hod_decisions
ALTER TABLE `thesis_hod_decisions`
CHANGE COLUMN `Note` `Comment` TEXT NULL;

-- Note: thesis_hod_decisions is already created in V25, so we don't recreate it here.
