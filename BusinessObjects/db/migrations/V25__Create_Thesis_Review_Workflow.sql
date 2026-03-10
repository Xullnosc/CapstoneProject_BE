-- Thesis review workflow using team mentors (MentorId / MentorId2) as reviewers.
CREATE TABLE IF NOT EXISTS `thesis_reviews` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `ThesisId` CHAR(36) NOT NULL,
  `ReviewerId` INT NOT NULL,
  `Decision` ENUM('Pass','Fail') NOT NULL,
  `Note` TEXT NULL,
  `ReviewedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_Review_Thesis_Reviewer` (`ThesisId`, `ReviewerId`),
  KEY `IX_Reviews_ThesisId` (`ThesisId`),
  KEY `IX_Reviews_ReviewerId` (`ReviewerId`),
  CONSTRAINT `FK_Reviews_Thesis` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
  CONSTRAINT `FK_Reviews_Reviewer` FOREIGN KEY (`ReviewerId`) REFERENCES `users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `thesis_hod_decisions` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `ThesisId` CHAR(36) NOT NULL,
  `HodId` INT NOT NULL,
  `Decision` ENUM('Pass','Fail') NOT NULL,
  `Note` TEXT NULL,
  `DecidedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_HodDecision_Thesis` (`ThesisId`),
  KEY `IX_HodDecisions_ThesisId` (`ThesisId`),
  KEY `IX_HodDecisions_HodId` (`HodId`),
  CONSTRAINT `FK_HodDecision_Thesis` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
  CONSTRAINT `FK_HodDecision_Hod` FOREIGN KEY (`HodId`) REFERENCES `users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Add a dedicated status for split-decision cases (1 pass / 1 fail) awaiting HOD final decision.
-- Keep existing values intact.
ALTER TABLE `thesis`
  MODIFY COLUMN `Status`
    ENUM(
      'Published',
      'Updated',
      'Need Update',
      'Reviewing',
      'HOD Reviewing',
      'Rejected',
      'Registered',
      'Cancelled',
      'On Mentor Inviting'
    )
    DEFAULT 'On Mentor Inviting';

