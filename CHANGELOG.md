# Changelog

All notable changes to the NeighborTools project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Added
- **Enhanced Payment Workflow and Navigation** (2025-01-21)
  - Added "Pay Now to Show Commitment" functionality for pending rentals in RentalDetails.razor
  - Implemented clickable tool names/captions across rental interfaces for easy navigation
  - Added proper cost calculation display in payment buttons using backend API calculations
  - Enhanced rental cost caching in MyRentals.razor to avoid repeated API calls
  - Improved user experience with meaningful explanations for optional upfront payments

### Changed
- **Documentation Enhancement** (2025-01-20)
  - Added Docker image names to CLAUDE.md for quick reference (`mysql:8.0`, `redis:7-alpine`)
  - Added configuration file locations and password storage information
  - Enhanced service configuration documentation with container names and file paths

### Fixed
- **Payment Dialog Cost Calculation** (2025-01-21)
  - Fixed critical bug where payment confirmation dialog showed costs multiplied by ~100
  - Resolved double fee calculation issue in PaymentConfirmationDialog.razor
  - Replaced PaymentBreakdownComponent with direct breakdown display to prevent redundant calculations
  - Updated dialog parameters to use pre-calculated values from backend API response
  - Ensured consistent cost display across rental request, payment dialog, and rental pages

- **Payment Cost Calculation Accuracy** (2025-01-21)
  - Replaced incorrect local cost calculation methods with proper backend API calls
  - Implemented `PaymentService.CalculateRentalCostAsync()` usage in RentalDetails.razor and MyRentals.razor
  - Fixed rate calculation logic to match backend Math.Ceiling implementation for weekly/monthly rates
  - Removed flawed local `CalculateRentalCost` methods that used incorrect integer division
  - Enhanced error handling for cost calculation failures with user-friendly feedback

- **Navigation and User Experience** (2025-01-21)
  - Made tool names clickable in both MyRentals.razor and RentalDetails.razor for easy tool details access
  - Added MudLink components with proper href routing to `/tools/{toolId}` for seamless navigation
  - Enhanced rental cards with clickable tool titles using Color.Primary styling for better visual indication
  - Improved user workflow by providing direct access to tool information from rental management pages
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