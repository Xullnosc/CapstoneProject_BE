CREATE TABLE chat_conversations (
    ConversationId  INT      NOT NULL AUTO_INCREMENT,
    User1Id         INT      NOT NULL COMMENT 'UserId nhỏ hơn (để đảm bảo unique pair)',
    User2Id         INT      NOT NULL COMMENT 'UserId lớn hơn',
    SemesterId      INT      NOT NULL COMMENT 'Conversation scoped theo semester',
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (ConversationId),
    UNIQUE KEY UQ_Conversation_Users_Semester (User1Id, User2Id, SemesterId),
    INDEX IX_Conversations_User1 (User1Id),
    INDEX IX_Conversations_User2 (User2Id),
    INDEX IX_Conversations_SemesterId (SemesterId),

    CONSTRAINT FK_Conversations_User1
        FOREIGN KEY (User1Id) REFERENCES users (UserID) ON DELETE CASCADE,
    CONSTRAINT FK_Conversations_User2
        FOREIGN KEY (User2Id) REFERENCES users (UserID) ON DELETE CASCADE,
    CONSTRAINT FK_Conversations_Semester
        FOREIGN KEY (SemesterId) REFERENCES semesters (SemesterID) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
