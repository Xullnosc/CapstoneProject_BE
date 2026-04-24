CREATE TABLE review_councils (
    id INT AUTO_INCREMENT PRIMARY KEY,
    semester_id INT NOT NULL,
    council_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    created_by INT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ReviewCouncils_Semesters FOREIGN KEY (semester_id) REFERENCES semesters(SemesterID)
);

CREATE TABLE review_council_members (
    council_id INT NOT NULL,
    lecturer_id INT NOT NULL,
    role VARCHAR(50) NOT NULL,
    PRIMARY KEY (council_id, lecturer_id),
    CONSTRAINT FK_ReviewCouncilMembers_Councils FOREIGN KEY (council_id) REFERENCES review_councils(id) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewCouncilMembers_Lecturers FOREIGN KEY (lecturer_id) REFERENCES lecturers(LecturerID)
);

CREATE TABLE review_council_teams (
    council_id INT NOT NULL,
    team_id INT NOT NULL,
    assigned_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (council_id, team_id),
    CONSTRAINT FK_ReviewCouncilTeams_Councils FOREIGN KEY (council_id) REFERENCES review_councils(id) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewCouncilTeams_Teams FOREIGN KEY (team_id) REFERENCES teams(TeamID)
);

CREATE TABLE review_schedules (
    id INT AUTO_INCREMENT PRIMARY KEY,
    council_id INT NOT NULL,
    review_round TINYINT NOT NULL,
    scheduled_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    meet_link VARCHAR(255),
    notified_at DATETIME,
    set_by_lecturer_id INT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ReviewSchedules_Councils FOREIGN KEY (council_id) REFERENCES review_councils(id) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewSchedules_Lecturers FOREIGN KEY (set_by_lecturer_id) REFERENCES lecturers(LecturerID)
);
