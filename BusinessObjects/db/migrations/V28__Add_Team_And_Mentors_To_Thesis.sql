-- Migration to add TeamId, MentorId1, and MentorId2 to thesis table
ALTER TABLE thesis
ADD COLUMN TeamId INT NULL,
ADD COLUMN MentorId1 INT NULL,
ADD COLUMN MentorId2 INT NULL;

-- Add foreign key constraints
ALTER TABLE thesis
ADD CONSTRAINT FK_Thesis_Teams_TeamId FOREIGN KEY (TeamId) REFERENCES teams(TeamId) ON DELETE SET NULL,
ADD CONSTRAINT FK_Thesis_Lecturers_MentorId1 FOREIGN KEY (MentorId1) REFERENCES lecturers(LecturerId) ON DELETE SET NULL,
ADD CONSTRAINT FK_Thesis_Lecturers_MentorId2 FOREIGN KEY (MentorId2) REFERENCES lecturers(LecturerId) ON DELETE SET NULL;
