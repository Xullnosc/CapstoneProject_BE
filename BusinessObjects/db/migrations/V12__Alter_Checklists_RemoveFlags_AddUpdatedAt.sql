-- V12: migrate existing checklists table to new schema
-- - remove IsCompleted flag (no longer used)
-- - remove DisplayOrder and its index
-- - add UpdatedAt timestamp (audit for changes)

-- 1) Drop IsCompleted (no longer used)
ALTER TABLE checklists
    DROP COLUMN IsCompleted;

-- 2) Drop index on DisplayOrder, then the column itself
ALTER TABLE checklists
    DROP INDEX IX_Checklist_DisplayOrder;

ALTER TABLE checklists
    DROP COLUMN DisplayOrder;

-- 3) Add UpdatedAt with auto-update behavior
ALTER TABLE checklists
    ADD COLUMN UpdatedAt DATETIME NULL
        DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP;

