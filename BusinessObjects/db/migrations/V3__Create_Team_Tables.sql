CREATE TABLE IF NOT EXISTS Teams (
    TeamId INT AUTO_INCREMENT PRIMARY KEY,
    TeamCode VARCHAR(50) UNIQUE NOT NULL,  -- Format: YYYY-[Semester]-XXX
    TeamName VARCHAR(255) NOT NULL,
    TeamAvatar VARCHAR(500),  -- URL to avatar image (default: FPT logo)
    Description TEXT,
    SemesterId INT NOT NULL,
    LeaderId INT NOT NULL,
    Status ENUM('Insufficient', 'Pending', 'Qualified', 'Disbanded') DEFAULT 'Insufficient',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId),
    FOREIGN KEY (LeaderId) REFERENCES Users(UserId),
    INDEX idx_semester (SemesterId),
    INDEX idx_leader (LeaderId),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS TeamMembers (
    TeamMemberId INT AUTO_INCREMENT PRIMARY KEY,
    TeamId INT NOT NULL,
    StudentId INT NOT NULL,
    Role ENUM('Leader', 'Member') DEFAULT 'Member',
    JoinedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (TeamId) REFERENCES Teams(TeamId) ON DELETE CASCADE,
    FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    UNIQUE KEY unique_team_student (TeamId, StudentId),
    INDEX idx_team (TeamId),
    INDEX idx_student (StudentId)
);

CREATE TABLE IF NOT EXISTS TeamInvitations (
    InvitationId INT AUTO_INCREMENT PRIMARY KEY,
    TeamId INT NOT NULL,
    StudentId INT NOT NULL,
    InvitedBy INT NOT NULL,
    Status ENUM('Pending', 'Accepted', 'Declined', 'Cancelled') DEFAULT 'Pending',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    RespondedAt DATETIME,
    FOREIGN KEY (TeamId) REFERENCES Teams(TeamId) ON DELETE CASCADE,
    FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    FOREIGN KEY (InvitedBy) REFERENCES Users(UserId),
    INDEX idx_team (TeamId),
    INDEX idx_student (StudentId),
    INDEX idx_status (Status)
);
