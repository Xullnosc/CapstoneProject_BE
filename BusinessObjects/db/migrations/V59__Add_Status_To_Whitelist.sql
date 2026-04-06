-- Database migration: V59__Add_Status_To_Whitelist.sql
-- Description: Adds a Status column to the Whitelist table for qualified/unqualified tracking.

ALTER TABLE Whitelist ADD Status NVARCHAR(50) NULL;


