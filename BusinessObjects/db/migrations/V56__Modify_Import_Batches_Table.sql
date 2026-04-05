-- Migration: Modify ImportBatches Table to add OriginalFileName
-- Purpose: add a new column to store the original file name representing the whitelist file imported.

ALTER TABLE ImportBatches
ADD COLUMN OriginalFileName VARCHAR(256) NULL AFTER FileUrl;
