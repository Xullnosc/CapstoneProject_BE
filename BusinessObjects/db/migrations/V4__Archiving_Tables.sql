-- 1. Add SemesterId to Whitelists (Live Table)
-- We assume SemesterId will link to Semesters table.
-- Since this is a new column on an existing table, we allow NULL initially or set default if needed.
-- However, for strict integrity, we should enforce it. For now, we allow NULL to avoid breaking existing data immediately,
-- or we assume existing data belongs to Semester 1 (if any). Let's make it NULLable for safety first, then User can backfill.

-- Check if SemesterId column exists in Whitelist table and add it if not
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Whitelist'
      AND COLUMN_NAME = 'SemesterId'
);

SET @sql_add_col := IF(
    @col_exists = 0,
    'ALTER TABLE Whitelist ADD COLUMN SemesterId INT NULL',
    'SELECT 1'
);

PREPARE stmt_add_col FROM @sql_add_col;
EXECUTE stmt_add_col;
DEALLOCATE PREPARE stmt_add_col;

-- Now add the foreign key constraint
SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Whitelist'
      AND CONSTRAINT_NAME = 'FK_Whitelist_Semester'
);

SET @sql := IF(
    @fk_exists = 0,
    'ALTER TABLE Whitelist
     ADD CONSTRAINT FK_Whitelist_Semester
     FOREIGN KEY (SemesterId)
     REFERENCES Semesters(SemesterId)',
    'SELECT 1'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;



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
