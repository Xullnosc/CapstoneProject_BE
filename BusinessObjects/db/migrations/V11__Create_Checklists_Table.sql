-- Checklist: Evaluation criteria for thesis proposals (HOD manages).
-- Each row = one criterion item.
CREATE TABLE IF NOT EXISTS checklists (
    ChecklistId INT NOT NULL AUTO_INCREMENT,
    Title VARCHAR(255) NOT NULL,
    Content VARCHAR(500) NOT NULL,
    IsCompleted BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ChecklistId),
    INDEX IX_Checklist_DisplayOrder (DisplayOrder)
);
