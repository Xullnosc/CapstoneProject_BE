CREATE TABLE IF NOT EXISTS SystemParameters (
    `Key` VARCHAR(255) PRIMARY KEY,
    `Value` LONGTEXT NOT NULL,
    `Description` VARCHAR(1000) NULL,
    `CreatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL
);

-- Insert some default configurations
INSERT INTO SystemParameters (`Key`, `Value`, `Description`) VALUES 
('FILE_SIZE_LIMIT_MB', '10', 'Maximum allowed file size for thesis or application uploads in Megabytes.'),
('MAX_TEAM_SIZE', '5', 'Maximum number of students permitted in a single capstone team.'),
('MIN_TEAM_SIZE', '4', 'Minimum number of students required to form a qualified team.'),
('THESIS_REGISTRATION_OPEN', 'true', 'Boolean flag indicating if students can currently propose or register for thesis topics.');
