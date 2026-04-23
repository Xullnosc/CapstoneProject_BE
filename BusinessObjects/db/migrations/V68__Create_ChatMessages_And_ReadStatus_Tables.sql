-- Bảng chat_messages: unified cho cả DM và Team Chat
CREATE TABLE chat_messages (
    MessageId       INT          NOT NULL AUTO_INCREMENT,
    -- Một trong hai phải có giá trị (DM hoặc Team)
    ConversationId  INT          NULL COMMENT 'FK nếu là DM giữa 2 người',
    TeamId          INT          NULL COMMENT 'FK nếu là Team Chat',
    SenderId        INT          NOT NULL,
    Content         TEXT         NOT NULL,
    MessageType     ENUM('text','image','file') NOT NULL DEFAULT 'text',
    AttachmentUrl   VARCHAR(500) NULL COMMENT 'URL Cloudinary nếu là file/image',
    AttachmentName  VARCHAR(255) NULL COMMENT 'Tên file gốc',
    IsDeleted       TINYINT(1)   NOT NULL DEFAULT 0,
    CreatedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (MessageId),
    INDEX IX_Messages_ConversationId_CreatedAt (ConversationId, CreatedAt),
    INDEX IX_Messages_TeamId_CreatedAt (TeamId, CreatedAt),
    INDEX IX_Messages_SenderId (SenderId),

    CONSTRAINT FK_Messages_Conversation
        FOREIGN KEY (ConversationId) REFERENCES chat_conversations (ConversationId)
        ON DELETE CASCADE,
    CONSTRAINT FK_Messages_Team
        FOREIGN KEY (TeamId) REFERENCES teams (TeamId)
        ON DELETE CASCADE,
    CONSTRAINT FK_Messages_Sender
        FOREIGN KEY (SenderId) REFERENCES users (UserID)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Bảng chat_read_status: theo dõi tin nhắn đã đọc
CREATE TABLE chat_read_status (
    StatusId        INT      NOT NULL AUTO_INCREMENT,
    UserId          INT      NOT NULL,
    ConversationId  INT      NULL COMMENT 'NULL nếu là Team Read',
    TeamId          INT      NULL COMMENT 'NULL nếu là DM Read',
    LastReadAt      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (StatusId),
    UNIQUE KEY UQ_ReadStatus_User_Conversation (UserId, ConversationId),
    UNIQUE KEY UQ_ReadStatus_User_Team (UserId, TeamId),
    INDEX IX_ReadStatus_UserId (UserId),

    CONSTRAINT FK_ReadStatus_User
        FOREIGN KEY (UserId) REFERENCES users (UserID) ON DELETE CASCADE,
    CONSTRAINT FK_ReadStatus_Conversation
        FOREIGN KEY (ConversationId) REFERENCES chat_conversations (ConversationId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ReadStatus_Team
        FOREIGN KEY (TeamId) REFERENCES teams (TeamId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
