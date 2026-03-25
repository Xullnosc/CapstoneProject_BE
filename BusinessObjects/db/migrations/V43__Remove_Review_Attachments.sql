-- V42: Remove Review Attachments
-- Remove the feature allowing reviewers to attach files to their review decisions.

-- 1. Drop the attachments table
DROP TABLE IF EXISTS `thesis_review_attachments`;

-- 2. Optional: Clean up orphaned files in comments
-- (No specific column needs dropping in comments table, as attachments were in their own table)
