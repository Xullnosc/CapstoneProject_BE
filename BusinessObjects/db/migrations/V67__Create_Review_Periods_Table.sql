CREATE TABLE review_periods (
    id INT AUTO_INCREMENT PRIMARY KEY,
    semester_id INT NOT NULL,
    review_round TINYINT NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    CONSTRAINT FK_ReviewPeriods_Semesters FOREIGN KEY (semester_id) REFERENCES semesters(SemesterID),
    CONSTRAINT UQ_Semester_Round UNIQUE (semester_id, review_round)
);
