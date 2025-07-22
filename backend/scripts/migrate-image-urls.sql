-- Migration script to convert existing image URLs to storage paths
-- Run this script manually after deploying the URL service changes

-- Update Bundle ImageUrls: Convert /api/files/download/images/file.png -> images/file.png
UPDATE Bundles 
SET ImageUrl = SUBSTRING(ImageUrl, 22)  -- Remove '/api/files/download/' prefix
WHERE ImageUrl LIKE '/api/files/download/%';

-- Update Tool ImageUrls in the JSON field
-- This is more complex due to JSON array structure - may need application-level migration
-- For now, we'll handle this in the UrlService with backward compatibility

SELECT 'Migration completed. Bundle ImageUrls updated.' AS Status;