
-- Migration: Add ImportBatches table to record import file metadata and versions
-- Purpose: store Cloudinary file URL, uploader, versioning and affected semester
CREATE TABLE IF NOT EXISTS ImportBatches (
	ImportBatchId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	FileUrl VARCHAR(1024) NOT NULL,
	UploadedBy VARCHAR(256) NULL,
	UploadedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	AffectedSemesterId INT NULL,
	Version INT NOT NULL DEFAULT 1,
	Notes VARCHAR(1024) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Optional: index to quickly find latest batch for a semester
CREATE INDEX IX_ImportBatches_AffectedSemesterId ON ImportBatches (AffectedSemesterId);

