-- V36: Add normalized thesis review timeline/comment/reply schema
-- V36 introduces the new source-of-truth review timeline model.

CREATE TABLE IF NOT EXISTS `thesis_review_events` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `ThesisId` CHAR(36) NOT NULL,
  `EventType` ENUM(
    'REVIEWER_ASSIGNED',
    'REVIEWER_DECISION',
    'HOD_FINAL_DECISION',
    'COMMENT_ADDED',
    'COMMENT_EDITED',
    'STATUS_CHANGED',
    'SYSTEM'
  ) NOT NULL,
  `ActorUserId` INT NOT NULL,
  `ActorRole` ENUM('REVIEWER', 'HOD', 'MENTOR', 'SYSTEM') NOT NULL,
  `Decision` ENUM('Pass', 'Fail') NULL,
  `PreviousDecision` ENUM('Pass', 'Fail') NULL,
  `SequenceNo` INT NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` DATETIME NULL,
  `UpdatedBy` INT NULL,
  `IsDeleted` BIT NOT NULL DEFAULT b'0',
  PRIMARY KEY (`Id`),
  KEY `IX_ReviewEvents_ThesisId` (`ThesisId`),
  KEY `IX_ReviewEvents_ActorUserId` (`ActorUserId`),
  KEY `IX_ReviewEvents_EventType` (`EventType`),
  KEY `IX_ReviewEvents_ThesisId_SequenceNo` (`ThesisId`, `SequenceNo`),
  KEY `IX_ReviewEvents_ThesisId_CreatedAt` (`ThesisId`, `CreatedAt`),
  CONSTRAINT `FK_ReviewEvents_Thesis` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewEvents_ActorUser` FOREIGN KEY (`ActorUserId`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ReviewEvents_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `users` (`UserID`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `thesis_review_comments` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `EventId` BIGINT NOT NULL,
  `ThesisId` CHAR(36) NOT NULL,
  `ParentCommentId` BIGINT NULL,
  `AuthorUserId` INT NOT NULL,
  `Body` LONGTEXT NOT NULL,
  `CommentType` ENUM('DECISION_RATIONALE', 'FOLLOW_UP', 'REPLY', 'SYSTEM_NOTE') NOT NULL DEFAULT 'FOLLOW_UP',
  `VisibilityScope` ENUM('PUBLIC', 'REVIEWERS_ONLY', 'HOD_ONLY') NOT NULL DEFAULT 'PUBLIC',
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` DATETIME NULL,
  `UpdatedBy` INT NULL,
  `IsDeleted` BIT NOT NULL DEFAULT b'0',
  PRIMARY KEY (`Id`),
  KEY `IX_ReviewComments_EventId` (`EventId`),
  KEY `IX_ReviewComments_ParentCommentId` (`ParentCommentId`),
  KEY `IX_ReviewComments_ThesisId_CreatedAt` (`ThesisId`, `CreatedAt`),
  KEY `IX_ReviewComments_AuthorUserId` (`AuthorUserId`),
  CONSTRAINT `FK_ReviewComments_Event` FOREIGN KEY (`EventId`) REFERENCES `thesis_review_events` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewComments_ParentComment` FOREIGN KEY (`ParentCommentId`) REFERENCES `thesis_review_comments` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewComments_Thesis` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewComments_AuthorUser` FOREIGN KEY (`AuthorUserId`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ReviewComments_UpdatedBy` FOREIGN KEY (`UpdatedBy`) REFERENCES `users` (`UserID`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `thesis_review_attachments` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `CommentId` BIGINT NOT NULL,
  `ThesisId` CHAR(36) NOT NULL,
  `FileUrl` VARCHAR(1000) NOT NULL,
  `FileName` VARCHAR(255) NOT NULL,
  `MimeType` VARCHAR(100) NULL,
  `FileSize` BIGINT NULL,
  `UploadedBy` INT NOT NULL,
  `UploadedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsDeleted` BIT NOT NULL DEFAULT b'0',
  PRIMARY KEY (`Id`),
  KEY `IX_ReviewAttachments_CommentId` (`CommentId`),
  KEY `IX_ReviewAttachments_ThesisId_UploadedAt` (`ThesisId`, `UploadedAt`),
  CONSTRAINT `FK_ReviewAttachments_Comment` FOREIGN KEY (`CommentId`) REFERENCES `thesis_review_comments` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewAttachments_Thesis` FOREIGN KEY (`ThesisId`) REFERENCES `thesis` (`ThesisId`) ON DELETE CASCADE,
  CONSTRAINT `FK_ReviewAttachments_UploadedBy` FOREIGN KEY (`UploadedBy`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
