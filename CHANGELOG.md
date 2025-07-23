# Changelog

All notable changes to the NeighborTools project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Fixed
- **Bundle Approval Status Display Bug** (2025-01-23)
  - **Resolved Data Inconsistency**: Fixed bundle approval status showing incorrectly as "rejected" when actually approved
  - **API Data Synchronization**: Ensured backend API serves current database approval status after restarts
  - **Frontend Display Accuracy**: Bundle approval indicators now correctly reflect actual approval state from database
  - **Investigation Complete**: Confirmed proper mapping of IsApproved, PendingApproval, and RejectionReason fields from entity to DTO
  - **Eliminated User Confusion**: "Request Re-approval" button no longer appears for already approved bundles
  - **Database Verification**: Confirmed "Complete Woodworking Project Kit" and other bundles show correct approval status

### Added
- **Bundle Tool Selection Dialog Fix** (2025-01-23)
  - **Fixed EditBundle Tool Selection**: Converted EditBundle.razor from problematic inline MudDialog to proper DialogService pattern
  - **MudBlazor 8.x Compatibility**: Resolved "Add Tools" button not opening dialog in bundle edit page
  - **Reusable ToolSelectorDialog**: Both CreateBundle and EditBundle now use shared Components.Bundles.ToolSelectorDialog component
  - **Eliminated DotNetObjectReference Issues**: Removed inline @bind-IsVisible pattern that caused JavaScript disposal errors
  - **Improved Code Architecture**: Consistent dialog management across bundle creation and editing workflows
  - **Enhanced Error Handling**: Better tool selection and management with cleaner UpdateBundleTools method
  - **Removed Debug Logging**: Cleaned up console logging from ToolSelectorDialog and EditBundle components

- **Comprehensive Tool Features System** (2025-01-22)
  - **Tool Ratings & Reviews**: Complete review system with star ratings, detailed feedback, and user interaction
  - **Tag-Based Categorization**: Comma-separated tag system with clickable navigation and search integration
  - **View Count Tracking**: Automatic view increment on tool details page with display statistics
  - **Featured Tools System**: Admin-configurable featured tool highlighting with dedicated showcase sections
  - **Advanced Tool Statistics**: AverageRating, ReviewCount, ViewCount fields with real-time calculations
  - **Interactive Star Rating Component**: Reusable StarRating.razor with hover effects, multiple sizes, and display modes
  - **Tag Management UI**: TagChips.razor component with clickable tags and flexible styling options
  - **Tool Reviews Interface**: ToolReviewsSection.razor with pagination, review creation dialog, and CRUD operations
  - **Featured Tools Showcase**: FeaturedToolsSection.razor with responsive grid and enhanced tool cards
  - **Enhanced Tool Details Page**: Integration of all new features with ratings display, tag navigation, and review section
  - **Sample Data Population**: Realistic sample data for 50+ tools with category-based tags, ratings, and reviews
  - **API Enhancement**: 6 new REST endpoints for reviews, featured tools, tags, search, and view tracking
  - **Advanced Search Capabilities**: Backend-ready search with tag filtering and comprehensive query support
  - **Complete Feature Parity**: Tools now match bundle system capabilities with equivalent functionality

- **Complete Bundle System Implementation** (2025-01-21)
  - **Bundle Creation & Management**: Comprehensive bundle builder with tool selection, quantity management, and project configuration
  - **Bundle Marketplace**: Advanced browsing with search, filtering, sorting, and responsive grid layout
  - **Bundle Rental System**: Complete rental workflow with availability checking, cost calculation, and approval management
  - **Advanced Availability Coordination**: Sophisticated algorithm checking availability across all bundle tools simultaneously
  - **Dynamic Pricing Engine**: Bundle discounts, security deposits, platform fees with real-time calculation
  - **Bundle Discovery Integration**: Featured bundles on homepage, navigation menu integration, user dashboard
  - **Comprehensive UI**: 5 dedicated pages (Browse, Details, Create, Edit, My Bundles) with responsive design
  - **Bundle Rental Dialog**: Interactive rental request with date selection, availability checking, and cost breakdown
  - **API Architecture**: 11 RESTful endpoints for complete bundle lifecycle management
  - **Database Schema**: Bundle, BundleTool, and BundleRental entities with proper relationships
  - **Bundle Analytics**: View counts, rental statistics, and performance metrics
  - **Multi-Owner Support**: Handle bundles containing tools from different owners with approval coordination
  - **Bundle Rental Approval Workflow**: Complete owner management interface in MyRentals.razor with third tab for bundle rental requests
  - **Approval/Rejection System**: Streamlined approve/reject actions with confirmation dialogs and reason tracking
  - **Real-time Status Updates**: Automatic refresh after bundle rental status changes with user feedback

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