-- V21: System credentials for HOD/Admin (username+password) and refresh tokens
-- system_user_credentials: links User (HOD/Admin) to login username and password hash
CREATE TABLE IF NOT EXISTS system_user_credentials (
    UserId INT NOT NULL PRIMARY KEY,
    Username VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_SystemUserCredentials_Username (Username),
    CONSTRAINT FK_SystemUserCredentials_Users FOREIGN KEY (UserId) REFERENCES users (UserID) ON DELETE CASCADE
);

-- refresh_tokens: store refresh token hashes for rotation/revocation
CREATE TABLE IF NOT EXISTS refresh_tokens (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    TokenHash VARCHAR(255) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    RevokedAt DATETIME NULL,
    KEY IX_RefreshTokens_UserId_Expires (UserId, ExpiresAt),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES users (UserID) ON DELETE CASCADE
);
