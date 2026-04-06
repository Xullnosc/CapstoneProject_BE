
-- 2. Drop the redundant IsReviewer column from users table
-- We check if it exists first (though in many SQL dialects we just drop it)
-- For MySQL/MariaDB:
SET @col_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'whitelist'
      AND COLUMN_NAME = 'IsReviewer'
);

SET @sql = IF(
    @col_exists > 0,
    'ALTER TABLE whitelist DROP COLUMN IsReviewer',
    'SELECT "Column does not exist"'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
