-- Migration to drop V30 columns from thesis and create ThesisReviews table
ALTER TABLE thesis
DROP FOREIGN KEY FK_Thesis_Lecturers_ReviewerId1,
DROP FOREIGN KEY FK_Thesis_Lecturers_ReviewerId2;

ALTER TABLE thesis
DROP COLUMN ReviewerId1,
DROP COLUMN ReviewerId2,
DROP COLUMN ReviewStatus1,
DROP COLUMN ReviewComment1,
DROP COLUMN ReviewFileUrl1,
DROP COLUMN ReviewStatus2,
DROP COLUMN ReviewComment2,
DROP COLUMN ReviewFileUrl2;

Drop TABLE IF EXISTS thesis_reviews;
-- Create ThesisReviews table
CREATE TABLE thesis_reviews (
    ReviewId INT AUTO_INCREMENT PRIMARY KEY,
    ThesisId VARCHAR(255) NOT NULL,
    ReviewerId INT NOT NULL,
    Status VARCHAR(50) NOT NULL,
    Comment TEXT NULL,
    FileUrl VARCHAR(500) NULL,
    ReviewDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ThesisReviews_Thesis FOREIGN KEY (ThesisId) REFERENCES thesis(ThesisId) ON DELETE CASCADE,
    CONSTRAINT FK_ThesisReviews_Lecturers FOREIGN KEY (ReviewerId) REFERENCES lecturers(LecturerId) ON DELETE CASCADE
);
