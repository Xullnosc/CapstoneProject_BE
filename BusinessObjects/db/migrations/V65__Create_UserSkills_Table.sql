CREATE TABLE user_skills (
    SkillId     INT          NOT NULL AUTO_INCREMENT,
    UserId      INT          NOT NULL,
    SkillTag    ENUM('FE','BE','Mobile','AI','Fullstack','Other') NOT NULL,
    SkillLevel  ENUM('Beginner','Intermediate','Advanced')        NOT NULL DEFAULT 'Beginner',
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (SkillId),
    UNIQUE KEY UQ_UserSkills_UserId_SkillTag (UserId, SkillTag),
    INDEX IX_UserSkills_UserId (UserId),
    INDEX IX_UserSkills_SkillTag (SkillTag),

    CONSTRAINT FK_UserSkills_Users
        FOREIGN KEY (UserId) REFERENCES users (UserID)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
