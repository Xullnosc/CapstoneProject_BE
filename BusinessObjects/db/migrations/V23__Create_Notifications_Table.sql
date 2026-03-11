-- Migration V21: Create Notifications table
CREATE TABLE IF NOT EXISTS Notifications (
    NotificationId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Type ENUM('TeamInvitation', 'ThesisUpdate', 'MentorChange', 'SemesterDeadline', 
              'ChecklistUpdate', 'HODAction', 'SystemAnnouncement', 'FormSubmission') NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Message TEXT NOT NULL,
    RelatedEntityType VARCHAR(50) NULL COMMENT 'Team, Thesis, Checklist, etc.',
    RelatedEntityId INT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    ReadAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Composite index for covering unread notifications query (userId + isRead + newest first)
    INDEX IX_Notifications_UserId_IsRead_CreatedAt (UserId, IsRead, CreatedAt DESC),
    
    -- Index for cleanup job efficiency (date-based deletion)
    INDEX IX_Notifications_CreatedAt (CreatedAt),
    
    -- Foreign key constraint with cascade delete (GDPR-friendly)
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
