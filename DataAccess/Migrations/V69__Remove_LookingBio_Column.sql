-- V69: Remove LookingBio column from account_detail
-- This simplifies the teaming discovery logic to be based purely on team membership.

ALTER TABLE account_detail DROP COLUMN LookingBio;
