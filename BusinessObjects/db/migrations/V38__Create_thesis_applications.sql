CREATE TABLE IF NOT EXISTS thesis_applications (
  Id          INT AUTO_INCREMENT PRIMARY KEY,
  ThesisId    CHAR(36) NOT NULL,
  TeamId      INT NOT NULL,
  Status      ENUM('Pending','Approved','Rejected','Cancelled') DEFAULT 'Pending',
  CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT FK_Applications_Thesis FOREIGN KEY (ThesisId) REFERENCES thesis(ThesisId) ON DELETE CASCADE,
  CONSTRAINT FK_Applications_Teams  FOREIGN KEY (TeamId) REFERENCES teams(TeamId) ON DELETE CASCADE,
  UNIQUE KEY UQ_Application_Thesis_Team (ThesisId, TeamId),
  INDEX IX_ThesisApplications_ThesisId (ThesisId),
  INDEX IX_ThesisApplications_TeamId (TeamId)
);
