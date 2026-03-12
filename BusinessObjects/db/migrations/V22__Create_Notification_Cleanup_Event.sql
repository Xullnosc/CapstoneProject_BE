-- Migration V22: Create MySQL Event for automatic notification cleanup
-- This event deletes notifications older than 90 days, running daily at 2 AM
-- Batch deletion (LIMIT 10000) prevents long-running locks

-- NOTE: The MySQL event scheduler must be enabled at the server level by a DBA:
--   SET GLOBAL event_scheduler = ON;
-- or in my.cnf: event_scheduler=ON

-- Drop existing event if it exists (for idempotency)
DROP EVENT IF EXISTS cleanup_old_notifications;

-- Create event to clean up notifications older than 90 days
CREATE EVENT IF NOT EXISTS cleanup_old_notifications
ON SCHEDULE EVERY 1 DAY 
STARTS '2026-03-08 02:00:00'  -- Starts tomorrow at 2 AM
ON COMPLETION PRESERVE
ENABLE
COMMENT 'Deletes notifications older than 90 days in batches to maintain performance'
DO
BEGIN
    DECLARE rows_deleted INT DEFAULT 0;
    
    -- Delete in batches to avoid long locks
    DELETE FROM Notifications 
    WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY)
    LIMIT 10000;
    
    -- Get number of rows affected
    SET rows_deleted = ROW_COUNT();
    
    -- Optional: Log the deletion (uncomment if you have a logging table)
    -- INSERT INTO cleanup_logs (EventName, RowsDeleted, ExecutedAt) 
    -- VALUES ('cleanup_old_notifications', rows_deleted, NOW());
END;
