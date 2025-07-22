-- Populate tool features with sample data
-- Only run the data population part since columns were created by EF migration

-- Update existing tools with sample data
UPDATE `Tools` 
SET `Tags` = CASE 
    WHEN `Category` = 'Power Tools' THEN 'power,electric,construction,drilling'
    WHEN `Category` = 'Hand Tools' THEN 'manual,precision,crafting,basic'
    WHEN `Category` = 'Garden Tools' THEN 'outdoor,garden,maintenance,landscaping'
    WHEN `Category` = 'Automotive' THEN 'car,repair,maintenance,automotive'
    WHEN `Category` = 'Kitchen' THEN 'kitchen,cooking,food,appliance'
    WHEN `Category` = 'Cleaning' THEN 'cleaning,maintenance,household,sanitation'
    ELSE 'general,utility,basic,tools'
END,
`ViewCount` = FLOOR(RAND() * 100) + 10,
`AverageRating` = ROUND(3.5 + (RAND() * 1.5), 2),
`ReviewCount` = FLOOR(RAND() * 20) + 2,
`IsFeatured` = CASE WHEN RAND() < 0.15 THEN 1 ELSE 0 END
WHERE `IsApproved` = 1;

-- Create sample reviews for existing tools with rating data
-- First batch: 5-star reviews
INSERT INTO `Reviews` (`Id`, `ToolId`, `ReviewerId`, `RevieweeId`, `Rating`, `Title`, `Comment`, `Type`, `CreatedAt`, `UpdatedAt`)
SELECT 
    UUID(),
    t.`Id` as `ToolId`,
    'sample-user-1',
    t.`OwnerId` as `RevieweeId`,
    5,
    'Excellent tool!',
    'This tool worked perfectly for my project. High quality and well-maintained.',
    0, -- ToolReview
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 30) DAY),
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 30) DAY)
FROM `Tools` t 
WHERE t.`IsApproved` = 1 
LIMIT 5;

-- Second batch: 4-star reviews  
INSERT INTO `Reviews` (`Id`, `ToolId`, `ReviewerId`, `RevieweeId`, `Rating`, `Title`, `Comment`, `Type`, `CreatedAt`, `UpdatedAt`)
SELECT 
    UUID(),
    t.`Id` as `ToolId`,
    'sample-user-2',
    t.`OwnerId` as `RevieweeId`,
    4,
    'Great tool, minor issues',
    'Really useful tool, got the job done. Owner was very helpful and responsive.',
    0, -- ToolReview
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 60) DAY),
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 60) DAY)
FROM `Tools` t 
WHERE t.`IsApproved` = 1 
AND t.`Id` NOT IN (SELECT DISTINCT `ToolId` FROM `Reviews` WHERE `ToolId` IS NOT NULL)
LIMIT 8;

-- Third batch: 3-star reviews
INSERT INTO `Reviews` (`Id`, `ToolId`, `ReviewerId`, `RevieweeId`, `Rating`, `Title`, `Comment`, `Type`, `CreatedAt`, `UpdatedAt`)
SELECT 
    UUID(),
    t.`Id` as `ToolId`,
    'sample-user-3',
    t.`OwnerId` as `RevieweeId`,
    3,
    'Decent tool',
    'Tool was okay, served its purpose. Could have been in better condition but worked fine.',
    0, -- ToolReview
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 90) DAY),
    DATE_SUB(NOW(), INTERVAL FLOOR(RAND() * 90) DAY)
FROM `Tools` t 
WHERE t.`IsApproved` = 1 
AND t.`Id` NOT IN (SELECT DISTINCT `ToolId` FROM `Reviews` WHERE `ToolId` IS NOT NULL)
LIMIT 6;

-- Update tool statistics based on actual reviews
UPDATE `Tools` t SET
    `AverageRating` = COALESCE((
        SELECT ROUND(AVG(r.`Rating`), 2) 
        FROM `Reviews` r 
        WHERE r.`ToolId` = t.`Id` AND r.`Type` = 0
    ), 0.00),
    `ReviewCount` = COALESCE((
        SELECT COUNT(*) 
        FROM `Reviews` r 
        WHERE r.`ToolId` = t.`Id` AND r.`Type` = 0
    ), 0)
WHERE t.`IsApproved` = 1;

-- Set some tools as featured (about 15% of approved tools) 
-- First get the count, then update
SET @featured_count = (SELECT FLOOR(COUNT(*) * 0.15) FROM `Tools` WHERE `IsApproved` = 1 AND `AverageRating` >= 4.0 AND `ReviewCount` >= 3);

UPDATE `Tools` 
SET `IsFeatured` = 1 
WHERE `IsApproved` = 1 
AND `AverageRating` >= 4.0 
AND `ReviewCount` >= 3
AND `Id` IN (
    SELECT `Id` FROM (
        SELECT `Id` FROM `Tools` 
        WHERE `IsApproved` = 1 
        AND `AverageRating` >= 4.0 
        AND `ReviewCount` >= 3
        ORDER BY RAND()
        LIMIT 3
    ) as temp_featured
);

-- Add some variety to tags for more realistic data
UPDATE `Tools` 
SET `Tags` = CONCAT(`Tags`, CASE 
    WHEN `IsFeatured` = 1 THEN ',featured,popular,recommended'
    WHEN `AverageRating` >= 4.5 THEN ',high-rated,quality,premium'
    WHEN `DailyRate` <= 10 THEN ',budget,affordable,economical'
    WHEN `DailyRate` >= 50 THEN ',premium,professional,advanced'
    ELSE ',standard,reliable'
END)
WHERE `IsApproved` = 1;