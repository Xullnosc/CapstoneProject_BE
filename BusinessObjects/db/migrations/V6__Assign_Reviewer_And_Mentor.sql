-- Add IsReviewer to Whitelist (Singular)
ALTER TABLE Whitelist ADD IsReviewer BIT NOT NULL DEFAULT 0;

-- Add IsReviewer to archived_whitelists (Snake case)
ALTER TABLE archived_whitelists ADD IsReviewer BIT NOT NULL DEFAULT 0;

-- Add MentorId to Teams (Plural)
ALTER TABLE Teams ADD MentorId INT NULL;
ALTER TABLE Teams ADD CONSTRAINT FK_Teams_Users_MentorId FOREIGN KEY (MentorId) REFERENCES Users(UserId);

-- Add MentorId to archived_teams (Snake case)
ALTER TABLE archived_teams ADD MentorId INT NULL;
