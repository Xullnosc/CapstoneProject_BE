CREATE TABLE review_questions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    council_id INT NOT NULL,
    review_round TINYINT NOT NULL,
    category VARCHAR(100) NULL,
    question_text TEXT NOT NULL,
    question_type VARCHAR(20) DEFAULT 'YesNo',
    priority VARCHAR(50) NULL,
    display_order INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_ReviewQuestions_Councils FOREIGN KEY (council_id) REFERENCES review_councils(id) ON DELETE CASCADE
);

CREATE TABLE review_question_results (
    question_id INT NOT NULL,
    team_id INT NOT NULL,
    review_round TINYINT NOT NULL,
    yn_value BOOLEAN NULL,
    grade_value VARCHAR(50) NULL,
    submitted_by INT NOT NULL,
    submitted_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (question_id, team_id, review_round, submitted_by),
    CONSTRAINT FK_ReviewQuestionResults_Questions FOREIGN KEY (question_id) REFERENCES review_questions(id) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewQuestionResults_Teams FOREIGN KEY (team_id) REFERENCES teams(TeamID) ON DELETE CASCADE,
    CONSTRAINT FK_ReviewQuestionResults_Users FOREIGN KEY (submitted_by) REFERENCES users(UserID)
);
