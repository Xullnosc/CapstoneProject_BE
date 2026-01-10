CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE, 
    Description NVARCHAR(250)
);
GO
-- 2. Bảng Whitelist (Thêm StudentCode và Campus)
CREATE TABLE Whitelist (
    WhitelistID INT PRIMARY KEY IDENTITY(1,1), -- Dùng ID làm khóa chính tốt hơn cho EF Core
    Email NVARCHAR(100) UNIQUE NOT NULL,
    StudentCode NVARCHAR(20), -- MSSV cho sinh viên, để trống nếu là Mentor
    FullName NVARCHAR(250),
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    Campus NVARCHAR(50),
    AddedDate DATETIME DEFAULT GETDATE()
);
GO
-- 3. Bảng Users (Đồng bộ với Whitelist)
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(100) UNIQUE NOT NULL,
    StudentCode NVARCHAR(20), -- Lưu MSSV để đối soát môn PMG sau này
    FullName NVARCHAR(250),
    Avatar NVARCHAR(MAX),
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    IsAuthorized BIT DEFAULT 0,
    Campus NVARCHAR(50),
    LastLogin DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO
