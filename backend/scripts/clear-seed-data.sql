-- Clear existing seed data to allow proper seeding with hashed passwords
USE toolssharing;

-- Delete in correct order to respect foreign key constraints
-- Start with dependent tables first

-- GDPR related tables
DELETE FROM CookieConsents WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM DataProcessingLogs WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM DataSubjectRequests WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM UserConsents WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

-- Reviews (depends on users, tools, rentals)
DELETE FROM Reviews WHERE ReviewerId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM Reviews WHERE RevieweeId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

-- Rentals (depends on tools and users)
DELETE FROM Rentals WHERE RenterId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM Rentals WHERE OwnerId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

-- Tool images (depends on tools)
DELETE FROM ToolImages WHERE ToolId IN (SELECT Id FROM Tools WHERE OwnerId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902'));

-- Tools (depends on users)
DELETE FROM Tools WHERE OwnerId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

-- ASP.NET Identity related tables
DELETE FROM AspNetUserRoles WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM AspNetUserClaims WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM AspNetUserLogins WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');
DELETE FROM AspNetUserTokens WHERE UserId IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

-- Finally, delete the users themselves
DELETE FROM AspNetUsers WHERE Id IN ('user1-guid-1234-5678-9012345678901', 'user2-guid-1234-5678-9012345678902');

COMMIT;