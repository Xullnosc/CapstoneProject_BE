-- Add MentorId2 to Teams
ALTER TABLE Teams ADD MentorId2 INT NULL;
ALTER TABLE Teams ADD CONSTRAINT FK_Teams_Users_MentorId2 FOREIGN KEY (MentorId2) REFERENCES Users(UserId);

-- Add MentorId2 to archived_teams
ALTER TABLE archived_teams ADD MentorId2 INT NULL;
