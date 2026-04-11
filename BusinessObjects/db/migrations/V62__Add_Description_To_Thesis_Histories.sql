-- Migration V62: Add Description column to thesis_histories
-- Store revision summary for each uploaded thesis revision.

ALTER TABLE `thesis_histories`
ADD COLUMN `Description` TEXT NULL AFTER `VersionNumber`;

