CREATE TABLE `thesis` (
  `ThesisId` char(36) NOT NULL DEFAULT (UUID()),
  `Title` varchar(255) NOT NULL,
  `ShortDescription` text,
  `UserId` int NOT NULL,
  `UpDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `UpdateDate` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `FileUrl` varchar(500),
  `Status` enum('Published','Updated','Need Update','Reviewing','Rejected', 'Registered') DEFAULT 'Reviewing',
  PRIMARY KEY (`ThesisId`),
  CONSTRAINT `fk_thesis_userid` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
