-- Migration: Update V42 to V43 as requested
-- Renamed V42__Remove_Review_Attachments.sql to V43__Remove_Review_Attachments.sql

-- New Migration: V44__Add_System_Settings.sql
CREATE TABLE `system_settings` (
    `setting_key` VARCHAR(100) NOT NULL,
    `setting_value` TEXT NOT NULL,
    `description` VARCHAR(255) DEFAULT NULL,
    PRIMARY KEY (`setting_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Seed initial support contact info
INSERT INTO `system_settings` (`setting_key`, `setting_value`, `description`) 
VALUES ('SupportEmail', 'longnx6@fe.edu.vn', 'Support email shown during login errors');

INSERT INTO `system_settings` (`setting_key`, `setting_value`, `description`) 
VALUES ('SupportPhone', '0905 764750', 'Support phone shown during login errors');
