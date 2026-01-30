INSERT INTO roles (RoleName, Description) VALUES
('HOD', 'Head of Department - Quản trị hệ thống, duyệt đề tài final'),
('Lecturer', 'Giảng viên'),
('Student', 'Sinh viên - Đăng ký đề tài'),
('Admin', 'Quản trị toàn bộ hệ thống')
ON DUPLICATE KEY UPDATE
Description = VALUES(Description);
