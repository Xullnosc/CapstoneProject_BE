CREATE TABLE `SystemErrorLogs` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Level` VARCHAR(20) NOT NULL,
    `Message` TEXT NOT NULL,
    `StackTrace` LONGTEXT NULL,
    `Timestamp` DATETIME NOT NULL
);
