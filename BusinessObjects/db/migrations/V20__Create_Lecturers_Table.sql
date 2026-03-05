-- Migration V20: Create Lecturers table
CREATE TABLE lecturers (
    LecturerId INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    FullName VARCHAR(255),
    Avatar TEXT,
    Campus VARCHAR(100),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Seed existing lecturers from whitelist to the new table if any
INSERT INTO lecturers (Email, FullName, Avatar, Campus, IsActive)
SELECT DISTINCT Email, FullName, Avatar, Campus, TRUE
FROM whitelist
WHERE RoleId = (SELECT RoleId FROM roles WHERE RoleName = 'Lecturer')
AND Email NOT IN (SELECT Email FROM lecturers);
