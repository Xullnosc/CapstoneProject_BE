-- V40: Refactor Checklist and Integrate with Review
-- 1. Remove Title from checklists
ALTER TABLE `checklists` DROP COLUMN  `Title`;

-- 2. Create join table for review checklist results
CREATE TABLE IF NOT EXISTS `thesis_review_checklist_results` (
  `EventId` BIGINT NOT NULL,
  `ChecklistId` INT NOT NULL,
  `IsChecked` BIT NOT NULL DEFAULT b'0',
  PRIMARY KEY (`EventId`, `ChecklistId`),
  CONSTRAINT `FK_ChecklistResults_Event` FOREIGN KEY (`EventId`) REFERENCES `thesis_review_events` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ChecklistResults_Checklist` FOREIGN KEY (`ChecklistId`) REFERENCES `checklists` (`ChecklistId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
