-- Migration to add ReviewerId1, ReviewerId2, and their respective status/comment columns
ALTER TABLE thesis
ADD COLUMN ReviewerId1 INT NULL,
ADD COLUMN ReviewerId2 INT NULL,
ADD COLUMN ReviewStatus1 VARCHAR(50) NULL,
ADD COLUMN ReviewComment1 TEXT NULL,
ADD COLUMN ReviewFileUrl1 VARCHAR(500) NULL,
ADD COLUMN ReviewStatus2 VARCHAR(50) NULL,
ADD COLUMN ReviewComment2 TEXT NULL,
ADD COLUMN ReviewFileUrl2 VARCHAR(500) NULL;

-- Add foreign key constraints for reviewers
ALTER TABLE thesis
ADD CONSTRAINT FK_Thesis_Lecturers_ReviewerId1 FOREIGN KEY (ReviewerId1) REFERENCES lecturers(LecturerId) ON DELETE SET NULL,
ADD CONSTRAINT FK_Thesis_Lecturers_ReviewerId2 FOREIGN KEY (ReviewerId2) REFERENCES lecturers(LecturerId) ON DELETE SET NULL;
