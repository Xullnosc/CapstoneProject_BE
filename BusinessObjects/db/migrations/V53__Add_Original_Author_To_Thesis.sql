ALTER TABLE thesis ADD COLUMN OriginalAuthorId INT;
ALTER TABLE thesis ADD CONSTRAINT fk_thesis_original_author FOREIGN KEY (OriginalAuthorId) REFERENCES users(UserID) ON DELETE SET NULL;
UPDATE thesis SET OriginalAuthorId = UserId;
