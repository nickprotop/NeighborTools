-- Migration: Add tool features (tags, views, reviews, ratings)
-- This adds the missing features that bundles have to the Tool table

-- Add new columns to Tools table
ALTER TABLE `Tools` 
ADD COLUMN `Tags` VARCHAR(1000) DEFAULT '' COMMENT 'Comma-separated tags',
ADD COLUMN `ViewCount` INT DEFAULT 0 COMMENT 'Number of times tool has been viewed',
ADD COLUMN `AverageRating` DECIMAL(3,2) DEFAULT 0.00 COMMENT 'Average rating from reviews',
ADD COLUMN `ReviewCount` INT DEFAULT 0 COMMENT 'Total number of reviews',
ADD COLUMN `IsFeatured` TINYINT(1) DEFAULT 0 COMMENT 'Whether tool is featured';

-- Create indexes for better performance
CREATE INDEX `idx_tools_tags` ON `Tools` (`Tags`);
CREATE INDEX `idx_tools_featured` ON `Tools` (`IsFeatured`);
CREATE INDEX `idx_tools_rating` ON `Tools` (`AverageRating`);
CREATE INDEX `idx_tools_view_count` ON `Tools` (`ViewCount`);

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
-- First, get some tools to create reviews for
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
UPDATE `Tools` 
SET `IsFeatured` = 1 
WHERE `IsApproved` = 1 
AND `AverageRating` >= 4.0 
AND `ReviewCount` >= 3
ORDER BY RAND()
LIMIT (SELECT FLOOR(COUNT(*) * 0.15) FROM (SELECT * FROM `Tools` WHERE `IsApproved` = 1) AS temp);

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

COMMIT;