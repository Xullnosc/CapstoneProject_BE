-- 1. Add SemesterId to Whitelists (Live Table)
-- We assume SemesterId will link to Semesters table.
-- Since this is a new column on an existing table, we allow NULL initially or set default if needed.
-- However, for strict integrity, we should enforce it. For now, we allow NULL to avoid breaking existing data immediately, 
-- or we assume existing data belongs to Semester 1 (if any). Let's make it NULLable for safety first, then User can backfill.
ALTER TABLE Whitelist
ADD COLUMN SemesterId INT NULL,
ADD CONSTRAINT FK_Whitelist_Semester FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId);

-- 2. Create ArchivedWhitelists Table
CREATE TABLE IF NOT EXISTS archived_whitelists (
    ArchivedWhitelistId INT AUTO_INCREMENT PRIMARY KEY,
    OriginalWhitelistId INT NOT NULL, -- Keep reference to original ID if needed, or just for audit
    StudentCode VARCHAR(50),
    Email VARCHAR(100),
    FullName VARCHAR(100),
    RoleId INT,
    Campus VARCHAR(50),
    SemesterId INT NOT NULL, -- The semester this whitelist belonged to
    ArchivedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    INDEX IX_ArchivedWhitelist_Semester (SemesterId),
    INDEX IX_ArchivedWhitelist_Student (StudentCode)
);

-- 3. Create ArchivedTeams Table
CREATE TABLE IF NOT EXISTS archived_teams (
    ArchivedTeamId INT AUTO_INCREMENT PRIMARY KEY,
    OriginalTeamId INT NOT NULL,
    TeamCode VARCHAR(50),
    TeamName VARCHAR(100),
    SemesterId INT NOT NULL,
    LeaderId INT NOT NULL,
    Status VARCHAR(50), -- "Qualified", "Disbanded", "Passed"
    ArchivedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    JsonData JSON NULL, -- Store full snapshot of members, topic, etc.
    
    INDEX IX_ArchivedTeam_Semester (SemesterId),
    INDEX IX_ArchivedTeam_Original (OriginalTeamId)
);
