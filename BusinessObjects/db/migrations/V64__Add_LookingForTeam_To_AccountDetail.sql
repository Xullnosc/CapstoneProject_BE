-- Thêm 2 cột vào account_detail
ALTER TABLE account_detail
    ADD COLUMN IsLookingForTeam TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Sinh viên đang tìm team hay không',
    ADD COLUMN LookingBio       VARCHAR(300) NULL
        COMMENT 'Mô tả ngắn của sinh viên: skills, mong muốn...';
