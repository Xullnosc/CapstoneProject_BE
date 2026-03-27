-- V39: Add 'AUTHOR' role to thesis_review_events ActorRole enum
ALTER TABLE `thesis_review_events` 
MODIFY COLUMN `ActorRole` ENUM('REVIEWER', 'HOD', 'MENTOR', 'AUTHOR', 'SYSTEM') NOT NULL;
