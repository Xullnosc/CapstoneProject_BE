-- V47__add_campus_support_for_multi_campus.sql

-- 1. Create campuses table
CREATE TABLE IF NOT EXISTS campuses (
    CampusId    INT            NOT NULL AUTO_INCREMENT,
    CampusCode  VARCHAR(20)    NOT NULL UNIQUE,
    CampusName  VARCHAR(100)   NOT NULL,
    IsActive    TINYINT(1)     NOT NULL DEFAULT 1,
    CreatedAt   DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (CampusId)
);

-- Seed comprehensive campus data
INSERT IGNORE INTO campuses (CampusId, CampusCode, CampusName, IsActive) VALUES
(1, 'HL', 'FU-Hòa Lạc', 1),
(2, 'DN', 'FU-Đà Nẵng', 1),
(3, 'HCM', 'FU-Hồ Chí Minh', 1),
(4, 'CT', 'FU-Cần Thơ', 1),
(5, 'QN', 'FU-Quy Nhơn', 1);

-- 2. Add CampusId and set default to 1 (Hòa Lạc) for existing tables
-- Note: Simplified syntax for standard MySQL compatibility

-- Semesters
ALTER TABLE semesters ADD COLUMN CampusId INT NULL;
UPDATE semesters SET CampusId = 1 WHERE CampusId IS NULL;
ALTER TABLE semesters MODIFY COLUMN CampusId INT NOT NULL;
ALTER TABLE semesters ADD CONSTRAINT FK_Semesters_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);

-- Teams
ALTER TABLE teams ADD COLUMN CampusId INT NULL;
UPDATE teams SET CampusId = 1 WHERE CampusId IS NULL;
ALTER TABLE teams MODIFY COLUMN CampusId INT NOT NULL;
ALTER TABLE teams ADD CONSTRAINT FK_Teams_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);

-- Thesis
ALTER TABLE thesis ADD COLUMN CampusId INT NULL;
UPDATE thesis SET CampusId = 1 WHERE CampusId IS NULL;
ALTER TABLE thesis MODIFY COLUMN CampusId INT NOT NULL;
ALTER TABLE thesis ADD CONSTRAINT FK_Thesis_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);

-- Users (CampusId is NULL for Super Admins)
ALTER TABLE users ADD COLUMN CampusId INT NULL;
UPDATE users SET CampusId = 1 WHERE RoleId IN (1, 2, 3); -- Gán HOD, Lecturer, Student về campus mặc định
-- Lưu ý: Role Admin (thường là ID 4 hoặc dựa theo bảng roles) được để NULL
ALTER TABLE users ADD CONSTRAINT FK_Users_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);

-- Lecturers
ALTER TABLE lecturers ADD COLUMN CampusId INT NULL;
UPDATE lecturers SET CampusId = 1 WHERE CampusId IS NULL;
ALTER TABLE lecturers MODIFY COLUMN CampusId INT NOT NULL;
ALTER TABLE lecturers ADD CONSTRAINT FK_Lecturers_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);

-- Whitelist
ALTER TABLE whitelist ADD COLUMN CampusId INT NULL;
UPDATE whitelist SET CampusId = 1 WHERE CampusId IS NULL;
ALTER TABLE whitelist MODIFY COLUMN CampusId INT NOT NULL;
ALTER TABLE whitelist ADD CONSTRAINT FK_Whitelist_Campus 
        FOREIGN KEY (CampusId) REFERENCES campuses(CampusId);
