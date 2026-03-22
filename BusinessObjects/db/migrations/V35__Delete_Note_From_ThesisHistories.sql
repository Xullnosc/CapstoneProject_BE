-- Migration V35: Delete Note column from thesis_histories
-- Consolidating schema to remove unused Note field in history for simplicity.

ALTER TABLE `thesis_histories`
DROP COLUMN  `Note`;
