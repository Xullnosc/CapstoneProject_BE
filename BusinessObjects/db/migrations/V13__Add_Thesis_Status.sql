ALTER TABLE `thesis` MODIFY COLUMN `Status` ENUM('Published','Updated','Need Update','Reviewing','Rejected', 'Registered', 'Cancelled') DEFAULT 'Reviewing';
