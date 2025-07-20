# Changelog

All notable changes to the NeighborTools project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- **Admin Account Migration Conflict** (2025-01-20)
  - Resolved duplicate admin account creation in database migrations
  - Removed redundant `AddEssentialAdminUser.cs` migration that was causing silent failures
  - Updated `InitializeEssentialSystemData.cs` migration with known password hash for `Admin123!`
  - Fixed authentication issue where admin password was unknown due to conflicting migrations
  - Admin account now correctly uses `admin@neighbortools.com` with password `Admin123!`

## Guidelines

### Entry Format
Each changelog entry should include:
- **Category**: Added, Changed, Deprecated, Removed, Fixed, Security
- **Title**: Brief description of the change
- **Date**: Completion date in YYYY-MM-DD format
- **Details**: Technical implementation details and impact
- **Context**: Why the change was necessary

### Categories
- **Added**: New features
- **Changed**: Changes in existing functionality  
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Vulnerability fixes

### Update Process
Every completed task should be documented here with:
1. Clear description of what was accomplished
2. Technical details of implementation
3. Impact on users or system functionality
4. Any breaking changes or migration requirements