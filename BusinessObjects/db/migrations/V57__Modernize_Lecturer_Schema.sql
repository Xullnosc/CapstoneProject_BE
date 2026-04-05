-- Drop redundant Campus (string) column from whitelist table
ALTER TABLE whitelist DROP COLUMN Campus;

-- Drop redundant Campus (string) column from lecturers table
ALTER TABLE lecturers DROP COLUMN Campus;

-- Add IsHod column to lecturers table to identify HODs in the mentor pool
ALTER TABLE lecturers ADD COLUMN IsHod TINYINT(1) DEFAULT 0;

-- Drop redundant Campus (string) column from users table
ALTER TABLE users DROP COLUMN Campus;
