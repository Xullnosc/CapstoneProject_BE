
-- 2. Drop the redundant IsReviewer column from users table
-- We check if it exists first (though in many SQL dialects we just drop it)
-- For MySQL/MariaDB:
ALTER TABLE whitelist DROP COLUMN IsReviewer;
