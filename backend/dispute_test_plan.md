# Dispute Management System Test Plan

## Overview
This document outlines the comprehensive testing workflow for the dispute management system that includes:
- Dispute creation and management
- File upload for evidence
- Status updates and notifications
- API layer fixes

## Test Scenarios

### 1. Authentication Setup
- Login with test user (john.doe@email.com / Password123!)
- Verify JWT token is obtained

### 2. Rental Creation (Prerequisite)
- Create a rental between two test users
- Approve the rental
- This provides the rental ID needed for dispute creation

### 3. Dispute Creation Testing
**Endpoint:** `POST /api/disputes`
**Test Cases:**
- Create dispute with valid rental ID
- Test all dispute types: PaymentIssue, ItemCondition, DeliveryIssue, ServiceQuality, Other
- Test all categories: Payment, ItemQuality, Communication, Delivery, Other
- Verify dispute is created with proper status (Open)
- Verify both parties receive notification emails

### 4. Evidence Upload Testing
**Endpoint:** `POST /api/disputes/{id}/evidence`
**Test Cases:**
- Upload single file with description
- Upload multiple files simultaneously
- Test file validation (size limits, file types)
- Verify files are stored correctly
- Test access control (only dispute parties can upload)
- Test invalid dispute ID
- Test large file handling

### 5. Dispute Status Updates
**Endpoint:** `PUT /api/disputes/{id}/status`
**Test Cases:**
- Update from Open to InReview
- Update from InReview to Resolved
- Test escalation to PayPal
- Test unauthorized status changes
- Verify notification emails are sent for each status change

### 6. Dispute Messages/Communication
**Endpoint:** `POST /api/disputes/{id}/messages`
**Test Cases:**
- Add message as dispute initiator
- Add message as other party
- Add admin message
- Test internal vs public messages
- Verify read/unread status

### 7. Admin Functionality
**Endpoints:** Various admin endpoints
**Test Cases:**
- Admin can view all disputes
- Admin can assign disputes
- Admin can add internal notes
- Admin can escalate disputes
- View dispute statistics

### 8. File Storage Service Testing
**Functionality:** `IFileStorageService`
**Test Cases:**
- File upload with validation
- File download with access control
- File deletion
- File URL generation with expiry
- Storage path security

### 9. Notification Service Testing
**Functionality:** `IDisputeNotificationService`
**Test Cases:**
- Dispute created notification
- Message notification
- Status change notification
- Escalation notification
- Resolution notification
- Evidence upload notification
- Overdue notification

### 10. Edge Cases and Error Handling
**Test Cases:**
- Non-existent dispute IDs
- Unauthorized access attempts
- Invalid file uploads
- Database connection issues
- Email service failures
- Large file uploads beyond limits

## API Endpoints to Test

### Dispute Management
```
GET    /api/disputes                    # Get user's disputes
POST   /api/disputes                    # Create new dispute
GET    /api/disputes/{id}               # Get dispute details
PUT    /api/disputes/{id}/status        # Update dispute status
POST   /api/disputes/{id}/messages      # Add message
GET    /api/disputes/{id}/messages      # Get messages
POST   /api/disputes/{id}/evidence      # Upload evidence
GET    /api/disputes/{id}/evidence      # Get evidence files
PUT    /api/disputes/{id}/escalate      # Escalate to PayPal
PUT    /api/disputes/{id}/resolve       # Resolve dispute
```

### Admin Endpoints
```
GET    /api/disputes/admin/pending      # Get pending disputes
GET    /api/disputes/admin/statistics   # Get dispute stats
PUT    /api/disputes/admin/{id}/assign  # Assign dispute
```

## Test Data Requirements

### Users
- john.doe@email.com (Dispute initiator)
- jane.smith@email.com (Other party)
- admin@toolssharing.com (Admin user)

### Tools
- Need at least one tool owned by jane.smith

### Rentals
- Active rental between john and jane
- Completed rental for testing post-rental disputes

## Expected Results

### Database Changes
- New dispute record in Disputes table
- Evidence files in DisputeEvidence table
- Messages in DisputeMessages table
- Notification records

### File System
- Evidence files stored in configured storage location
- Proper file naming and organization

### Email Notifications
- Professional email templates sent
- Proper recipient targeting
- Template variables populated correctly

### API Responses
- Consistent ApiResponse<T> format
- Proper error handling and status codes
- Security validation (authorization checks)

## Security Validation
- Only dispute parties can access dispute details
- Only authorized users can upload evidence
- File upload validation prevents malicious files
- API endpoints require proper authentication
- Admin functions require admin role

## Performance Considerations
- File upload handling for large files
- Database query performance for dispute lists
- Email notification processing
- File storage scalability