SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Whitelist'
      AND COLUMN_NAME = 'SemesterId'
);

SET @sql_add_col := IF(
    @col_exists = 0,
    'ALTER TABLE Whitelist ADD COLUMN SemesterId INT NULL',
    'SELECT 1'
);

PREPARE stmt_add_col FROM @sql_add_col;
EXECUTE stmt_add_col;
DEALLOCATE PREPARE stmt_add_col;
