-- Initialize the tools sharing database
CREATE DATABASE IF NOT EXISTS toolssharing;
USE toolssharing;

-- Set character set
ALTER DATABASE toolssharing CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user if not exists
CREATE USER IF NOT EXISTS 'toolsuser'@'%' IDENTIFIED BY 'ToolsPassword123!';
GRANT ALL PRIVILEGES ON toolssharing.* TO 'toolsuser'@'%';
FLUSH PRIVILEGES;