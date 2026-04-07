-- Migration to rename StudentId to ReceiverId in teaminvitation table
ALTER TABLE teaminvitations CHANGE COLUMN studentid receiverid INT NOT NULL;
