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
-- Removed IF EXISTS for columns as it's not supported in some versions.
-- If the column doesn't exist, this script might fail; in that case, remove the DROP line.
-- We ADD it fresh to ensure it's BIT type.
ALTER TABLE `lecturers`
ADD COLUMN `IsReviewer` BIT NOT NULL DEFAULT 0;

-- 4. Clean up Whitelist table
-- Only run this if you know IsReviewer was previously added to whitelist.
-- ALTER TABLE `whitelist` DROP COLUMN `IsReviewer`;

-- 5. Trigger to ensure IsActive=0 => IsReviewer=0
DELIMITER //
DROP TRIGGER IF EXISTS `trg_lecturer_deactivate` //
CREATE TRIGGER `trg_lecturer_deactivate` BEFORE UPDATE ON `lecturers`
FOR EACH ROW
BEGIN
    IF NEW.IsActive = 0 THEN
        SET NEW.IsReviewer = 0;
    END IF;
END //
DELIMITER ;

-- 6. Rename Note to Comment in thesis_hod_decisions
-- Using CHANGE which is more standard for renaming and type definition.
-- It will rename Note to Comment and set its type to TEXT.
ALTER TABLE `thesis_hod_decisions`
CHANGE `Note` `Comment` TEXT NULL;

-- Note: thesis_hod_decisions is already created in V25, so we don't recreate it here.
