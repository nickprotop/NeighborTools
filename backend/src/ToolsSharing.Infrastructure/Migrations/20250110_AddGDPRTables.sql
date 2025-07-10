-- GDPR Compliance Migration
-- Date: 2025-01-10
-- Description: Add tables for GDPR compliance including consent management, data subject rights, and audit trails

-- User consent management
CREATE TABLE UserConsents (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    ConsentType ENUM('cookies', 'marketing', 'analytics', 'data_processing', 'financial_data', 'location_data') NOT NULL,
    ConsentGiven BOOLEAN NOT NULL,
    ConsentDate DATETIME NOT NULL,
    ConsentSource VARCHAR(100) NOT NULL, -- 'registration', 'cookie_banner', 'settings', 'api'
    ConsentVersion VARCHAR(20) NOT NULL, -- Version of privacy policy when consent was given
    IPAddress VARCHAR(45) NOT NULL,
    UserAgent VARCHAR(500) NULL,
    WithdrawnDate DATETIME NULL,
    WithdrawalReason VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_consent_type (UserId, ConsentType),
    INDEX idx_consent_date (ConsentDate),
    INDEX idx_withdrawal_date (WithdrawnDate)
);

-- Data processing activities log (Article 30 requirement)
CREATE TABLE DataProcessingLog (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    ActivityType VARCHAR(100) NOT NULL, -- 'account_creation', 'tool_listing', 'rental_request', 'payment_processing'
    DataCategories JSON NOT NULL, -- ['personal_data', 'financial_data', 'location_data', 'usage_data']
    ProcessingPurpose VARCHAR(500) NOT NULL,
    LegalBasis ENUM('consent', 'contract', 'legal_obligation', 'vital_interests', 'public_task', 'legitimate_interests') NOT NULL,
    DataSources JSON NOT NULL, -- Where data came from
    DataRecipients JSON NULL, -- Who data is shared with (Stripe, PayPal, etc.)
    RetentionPeriod VARCHAR(100) NOT NULL, -- How long data is kept
    ProcessingDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IPAddress VARCHAR(45) NOT NULL,
    UserAgent VARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_activity (UserId, ActivityType),
    INDEX idx_processing_date (ProcessingDate)
);

-- Data subject requests (Article 15-22 rights)
CREATE TABLE DataSubjectRequests (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    RequestType ENUM('access', 'rectification', 'erasure', 'portability', 'restriction', 'objection') NOT NULL,
    RequestDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RequestDetails TEXT NULL,
    Status ENUM('pending', 'in_progress', 'completed', 'rejected', 'partially_completed') NOT NULL DEFAULT 'pending',
    ResponseDate DATETIME NULL,
    ResponseDetails TEXT NULL,
    CompletionDate DATETIME NULL,
    ProcessedByUserId INT NULL, -- Admin who processed the request
    DataExportPath VARCHAR(500) NULL, -- Path to exported data file
    VerificationMethod VARCHAR(100) NULL, -- How identity was verified
    RejectionReason TEXT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProcessedByUserId) REFERENCES Users(Id) ON DELETE SET NULL,
    INDEX idx_user_request_type (UserId, RequestType),
    INDEX idx_request_status (Status),
    INDEX idx_request_date (RequestDate)
);

-- Privacy policy versions
CREATE TABLE PrivacyPolicyVersions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Version VARCHAR(20) NOT NULL UNIQUE,
    Content TEXT NOT NULL,
    EffectiveDate DATETIME NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    INDEX idx_version (Version),
    INDEX idx_effective_date (EffectiveDate)
);

-- Cookie consent tracking
CREATE TABLE CookieConsents (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    SessionId VARCHAR(255) NOT NULL, -- For anonymous users
    UserId INT NULL, -- For logged-in users
    CookieCategory ENUM('essential', 'functional', 'analytics', 'marketing') NOT NULL,
    ConsentGiven BOOLEAN NOT NULL,
    ConsentDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpiryDate DATETIME NOT NULL,
    IPAddress VARCHAR(45) NOT NULL,
    UserAgent VARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_session_category (SessionId, CookieCategory),
    INDEX idx_user_category (UserId, CookieCategory),
    INDEX idx_consent_date (ConsentDate),
    INDEX idx_expiry_date (ExpiryDate)
);

-- Data retention policies
CREATE TABLE DataRetentionPolicies (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    DataType VARCHAR(100) NOT NULL UNIQUE, -- 'user_account', 'financial_data', 'audit_logs'
    RetentionPeriodMonths INT NOT NULL,
    LegalBasis VARCHAR(500) NOT NULL,
    DeletionCriteria TEXT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_data_type (DataType),
    INDEX idx_active (IsActive)
);

-- Automated data deletion log
CREATE TABLE DataDeletionLog (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NULL, -- NULL for system-wide cleanups
    DeletionType ENUM('user_request', 'retention_policy', 'account_closure') NOT NULL,
    DataTypes JSON NOT NULL, -- Types of data deleted
    DeletionDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DeletedRecordsCount INT NOT NULL,
    InitiatedBy INT NULL, -- Admin user who initiated
    AutomatedProcess BOOLEAN NOT NULL DEFAULT FALSE,
    VerificationHash VARCHAR(255) NOT NULL, -- For audit integrity
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    FOREIGN KEY (InitiatedBy) REFERENCES Users(Id) ON DELETE SET NULL,
    INDEX idx_deletion_date (DeletionDate),
    INDEX idx_deletion_type (DeletionType)
);

-- Enhance existing Users table for GDPR
ALTER TABLE Users 
ADD COLUMN DataProcessingConsent BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN MarketingConsent BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN DataRetentionDate DATETIME NULL, 
ADD COLUMN LastConsentUpdate DATETIME NULL,
ADD COLUMN GDPROptOut BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN DataPortabilityRequested BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN AnonymizationDate DATETIME NULL;

-- Enhance existing FinancialAuditLog for GDPR
ALTER TABLE FinancialAuditLog 
ADD COLUMN DataCategories JSON NULL,
ADD COLUMN LegalBasis VARCHAR(100) NULL,
ADD COLUMN ConsentReference INT NULL;

-- Add foreign key constraint for consent reference
ALTER TABLE FinancialAuditLog 
ADD CONSTRAINT fk_consent_reference 
FOREIGN KEY (ConsentReference) REFERENCES UserConsents(Id) ON DELETE SET NULL;

-- Insert default retention policies
INSERT INTO DataRetentionPolicies (DataType, RetentionPeriodMonths, LegalBasis, DeletionCriteria) VALUES
('user_account', 24, 'Legitimate interests for fraud prevention', 'Delete 24 months after account closure unless active rentals exist'),
('financial_data', 84, 'Legal obligation (tax records)', 'Delete 7 years after last financial transaction'),
('audit_logs', 84, 'Legal obligation (financial regulations)', 'Delete 7 years after creation unless part of ongoing investigation'),
('rental_history', 36, 'Contract performance and legitimate interests', 'Delete 3 years after rental completion'),
('cookie_data', 12, 'Consent or legitimate interests', 'Delete 12 months after last activity'),
('marketing_data', 36, 'Consent', 'Delete immediately upon consent withdrawal or 3 years of inactivity');

-- Insert initial privacy policy version (placeholder)
INSERT INTO PrivacyPolicyVersions (Version, Content, EffectiveDate, CreatedBy, IsActive) VALUES
('1.0', 'GDPR-compliant privacy policy content will be added here', NOW(), 1, TRUE);