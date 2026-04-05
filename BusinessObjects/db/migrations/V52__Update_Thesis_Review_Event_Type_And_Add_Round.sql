-- V40: Rename HOD_FINAL_DECISION -> FINAL_DECISION and add Round column to thesis_review_events
-- FINAL_DECISION is now used for both the aggregated reviewer decision and the HOD finalization.
-- Round tracks which review iteration produced the event (1 = first review cycle, 2 = second, etc.)

ALTER TABLE `thesis_review_events`
  MODIFY COLUMN `EventType` ENUM(
    'REVIEWER_ASSIGNED',
    'REVIEWER_DECISION',
    'FINAL_DECISION',
    'COMMENT_ADDED',
    'COMMENT_EDITED',
    'STATUS_CHANGED',
    'SYSTEM'
  ) NOT NULL;

ALTER TABLE `thesis_review_events`
  ADD COLUMN `Round` INT NULL AFTER `SequenceNo`;
