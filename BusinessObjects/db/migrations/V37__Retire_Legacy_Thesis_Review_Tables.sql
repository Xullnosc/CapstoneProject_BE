-- V37: Retire legacy horizontal review tables in favor of timeline model.
-- New source of truth: thesis_review_events, thesis_review_comments, thesis_review_attachments.

DROP TABLE IF EXISTS `thesis_hod_decisions`;
DROP TABLE IF EXISTS `thesis_reviews`;
