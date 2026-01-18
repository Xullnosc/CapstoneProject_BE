CREATE TABLE IF NOT EXISTS `semesters` (
  `SemesterID` int NOT NULL AUTO_INCREMENT,
  `SemesterName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StartDate` datetime NOT NULL,
  `EndDate` datetime NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`SemesterID`)
);

CREATE TABLE IF NOT EXISTS `roles` (
  `RoleID` int NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`RoleID`),
  UNIQUE KEY `UQ__Roles__8A2B6160E9B78D92` (`RoleName`)
);

CREATE TABLE IF NOT EXISTS `whitelist` (
  `WhitelistID` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StudentCode` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FullName` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `RoleID` int DEFAULT NULL,
  `Campus` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AddedDate` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`WhitelistID`),
  UNIQUE KEY `UQ__Whitelis__A9D10534BDF4FDF3` (`Email`),
  KEY `IX_Whitelist_RoleID` (`RoleID`),
  CONSTRAINT `FK__Whitelist__RoleI__5070F446` FOREIGN KEY (`RoleID`) REFERENCES `roles` (`RoleID`)
);

CREATE TABLE IF NOT EXISTS `users` (
  `UserID` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StudentCode` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `FullName` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Avatar` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RoleID` int DEFAULT NULL,
  `IsAuthorized` tinyint(1) DEFAULT '0',
  `Campus` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LastLogin` datetime DEFAULT NULL,
  `CreatedAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserID`),
  UNIQUE KEY `UQ__Users__A9D105343BD5A87E` (`Email`),
  KEY `IX_Users_RoleID` (`RoleID`),
  CONSTRAINT `FK__Users__RoleID__5535A963` FOREIGN KEY (`RoleID`) REFERENCES `roles` (`RoleID`)
);
