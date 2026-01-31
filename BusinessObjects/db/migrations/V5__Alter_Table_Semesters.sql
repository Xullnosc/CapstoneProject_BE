ALTER TABLE `semesters`
  ADD `SemesterCode` varchar(50)
      CHARACTER SET utf8mb4
      COLLATE utf8mb4_0900_ai_ci
      NOT NULL,
  ADD UNIQUE KEY `UQ_Semesters_SemesterCode` (`SemesterCode`);
