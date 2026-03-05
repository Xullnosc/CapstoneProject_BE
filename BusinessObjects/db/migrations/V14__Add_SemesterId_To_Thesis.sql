-- Add SemesterId column to thesis table
ALTER TABLE `thesis` 
ADD COLUMN `SemesterId` int NULL;

-- Add foreign key constraint
ALTER TABLE `thesis` 
ADD CONSTRAINT `fk_thesis_semester` FOREIGN KEY (`SemesterId`) REFERENCES `semesters` (`SemesterID`) ON DELETE SET NULL;

-- Create index for performance
CREATE INDEX `idx_thesis_semester` ON `thesis` (`SemesterId`);
