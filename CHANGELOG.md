# Changelog

All notable changes to the NeighborTools project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Fixed
- **Complete MudBlazor Theme System Implementation** (2025-01-23)
  - **Fixed Theme Toggle Functionality**: Resolved non-functional theme toggle component by implementing missing ToggleTheme() method
  - **Fixed Dark Mode Background Issue**: Updated MainLayout.razor CSS to use theme-aware variables instead of hardcoded light colors
  - **Fixed Dark Mode Button Text Visibility**: Implemented dynamic CSS variable updates to ensure proper button text colors in both light and dark modes
  - **Dynamic CSS Variable Updates**: Added UpdateCssVariablesAsync() method to automatically update CSS custom properties when theme changes
  - **Proper Theme Architecture**: Implemented unified MudTheme with both PaletteLight and PaletteDark, controlled by IsDarkMode parameter on MudThemeProvider
  - **Enhanced Theme Colors**: Updated theme palette to use modern design system colors matching app.css color scheme
  - **Fixed Header/Footer Theming**: Updated top-header and app-footer to use var(--mud-palette-surface) and var(--mud-palette-divider) for proper dark mode support
  - **Comprehensive Color Mapping**: Updated --mud-text-primary, --mud-text-secondary, and action colors to ensure all UI elements are visible in both themes
  - **Cleaned Debug Code**: Removed all console logging statements from theme system for production readiness
  - **Theme Persistence**: Maintained localStorage persistence with system preference detection fallback
  - **Component Integration**: Proper ThemeToggle component with tooltip and smooth icon animations
  - **Cross-Component Updates**: Theme changes now properly update across all components via event-driven architecture

### Fixed
- **Entity Framework LINQ Query Translation Error** (2025-01-23)
  - **Fixed Complex Query Translation Issue**: Resolved critical error in MessageService.GetConversationByIdAsync where complex nested Include operations couldn't be translated to SQL
  - **Split Query Architecture**: Divided problematic single query into two separate database queries to avoid Entity Framework translation limitations
  - **Improved Message Loading**: First query retrieves conversation details, second query loads messages separately for better database compatibility
  - **Enhanced Error Handling**: Eliminated "The LINQ expression... could not be translated" errors that prevented conversation loading
  - **DTO Mapping Fix**: Corrected MessageDto type usage in ConversationDetailsDto to ensure proper data structure
  - **Better Performance**: Simplified queries reduce database complexity and improve conversation loading reliability
  - **User Experience Restoration**: Users can now successfully view message conversations without encountering translation errors

- **Navigation System Complete Refactoring** (2025-01-23)
  - **Complete Architecture Overhaul**: Replaced complex multi-component navigation system with clean two-component design (AppNavigation.razor + NavItem.razor)
  - **Fixed Desktop Sidebar Width**: Corrected oversized desktop sidebar to appropriate 280px (expanded) / 72px (collapsed) dimensions
  - **Fixed Desktop Collapse Functionality**: Restored working sidebar toggle with localStorage persistence and proper CSS transitions
  - **Fixed Hamburger Menu Visibility**: Resolved hamburger menu not appearing in mobile view through CSS media query implementation
  - **CSS-Driven Responsive Design**: Replaced unreliable JavaScript detection with robust CSS media queries (@media breakpoints at 1200px)
  - **Enhanced State Management**: Simplified navigation state to two boolean values with mobile-first fallback defaults
  - **Improved Error Resilience**: Navigation now defaults to mobile mode if screen detection fails, ensuring hamburger menu always appears when needed
  - **Unified Component Architecture**: Single AppNavigation.razor handles all screen sizes with conditional rendering patterns
  - **Reusable NavItem Component**: Created flexible NavItem.razor with badge support, tooltips, and responsive behavior
  - **Better MudBlazor 8.x Integration**: Proper EventCallback patterns and consistent component composition
  - **Performance Optimization**: Reduced JavaScript overhead by moving responsive behavior to CSS-only implementation

- **Navigation Restructure & Message Enhancement** (2025-01-23)
  - **Moved Profile Menu Items**: Relocated "My Tools" and "My Rentals" from main navigation to user profile dropdown menu
  - **Conditional Messages Display**: Messages now appear in top navigation only when there are unread messages with red badge count
  - **Profile Menu Messages**: Messages always available in profile dropdown with red chip indicator when unread messages exist
  - **Enhanced Mobile Navigation**: Updated mobile drawer navigation to match desktop structure with proper message count display
  - **Improved Navigation Focus**: Main navigation now shows only essential items (Browse Tools, Messages if unread, Disputes if active)
  - **Consistent UX Patterns**: Both desktop and mobile navigation follow same conditional display logic for optimal user experience
  - **MudBlazor 8.x Compliance**: Fixed MudChip type inference errors by adding explicit T="string" parameter

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