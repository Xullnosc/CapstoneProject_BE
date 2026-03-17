-- Account detail: thông tin bổ sung cho user (sinh viên) - thay thế V32
CREATE TABLE IF NOT EXISTS account_detail (
    AccountDetailId INT NOT NULL AUTO_INCREMENT,
    UserId INT NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    GithubLink VARCHAR(255) NULL,
    LinkedInLink VARCHAR(255) NULL,
    FacebookLink VARCHAR(255) NULL,
    DateOfBirth DATE NULL,
    Gender VARCHAR(20) NULL,
    Address VARCHAR(500) NULL,
    Major VARCHAR(100) NULL,
    PersonalId VARCHAR(20) NULL,
    PlaceOfBirth VARCHAR(200) NULL,
    EnrollmentYear INT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (AccountDetailId),
    UNIQUE KEY UQ_AccountDetail_UserId (UserId),
    CONSTRAINT FK_AccountDetail_Users FOREIGN KEY (UserId) REFERENCES users(UserID) ON DELETE CASCADE
);
