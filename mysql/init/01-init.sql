# mysql/init/01-init.sql
SET GLOBAL time_zone = '+00:00';    -- Configura zona horaria UTC
SET time_zone = '+00:00';           -- Configura sesión en UTC
CREATE DATABASE IF NOT EXISTS `DA`;
USE `DA`;

-- Create admin user with appropriate permissions
CREATE USER IF NOT EXISTS 'da_user'@'%' IDENTIFIED BY 'da_password';
GRANT ALL PRIVILEGES ON DA.* TO 'da_user'@'%';
FLUSH PRIVILEGES;

-- Set character set and collation
ALTER DATABASE `DA` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;