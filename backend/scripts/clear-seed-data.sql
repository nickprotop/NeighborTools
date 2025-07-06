-- Clear existing seed data to allow proper seeding with hashed passwords
USE toolssharing;

-- Delete in correct order to respect foreign key constraints
DELETE FROM Reviews;
DELETE FROM Rentals;
DELETE FROM ToolImages;
DELETE FROM Tools;
DELETE FROM AspNetUsers WHERE Id IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

COMMIT;