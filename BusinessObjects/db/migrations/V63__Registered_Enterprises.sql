CREATE TABLE IF NOT EXISTS `RegisteredEnterprises` (
    `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `EnterpriseName` VARCHAR(255) NOT NULL UNIQUE,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `RegisteredEnterprises` (`EnterpriseName`)
SELECT DISTINCT `EnterpriseName` 
FROM `thesis` 
WHERE `EnterpriseName` IS NOT NULL AND `EnterpriseName` != '';
