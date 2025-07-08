# TODO: Basic Commission System (MVP)

## Overview
Implement a minimal viable commission system to enable basic monetization of tool rentals. This serves as the foundation for the complete monetization platform.

## Scope (MVP Only)
- Simple rental pricing
- Fixed commission rate (10%)
- Basic Stripe integration
- Manual payout process
- Essential reporting

## Out of Scope (Future TODOs)
- Multiple payment providers
- Dynamic commission rates
- Automated payouts
- Subscription tiers
- Advanced analytics
- Tax integration
- Mobile payments

## Database Changes

### New Tables
```sql
-- Rental pricing
ALTER TABLE Rentals ADD COLUMN DailyRate DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN CommissionAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN OwnerEarnings DECIMAL(10,2) NOT NULL DEFAULT 0;

-- Payment tracking
CREATE TABLE RentalPayments (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    RentalId INT NOT NULL,
    StripePaymentIntentId VARCHAR(255) NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Status VARCHAR(50) NOT NULL, -- pending, completed, failed, refunded
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    FOREIGN KEY (RentalId) REFERENCES Rentals(Id)
);

-- Tool pricing
ALTER TABLE Tools ADD COLUMN DailyRate DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Tools ADD COLUMN IsAvailableForRent BOOLEAN NOT NULL DEFAULT TRUE;
```

## Backend Implementation

### 1. Rental Pricing Logic
- [ ] Add pricing fields to `Tool` entity
- [ ] Update `CreateRentalCommand` to calculate total cost
- [ ] Add commission calculation service (fixed 10% rate)
- [ ] Update rental approval to trigger payment

### 2. Basic Stripe Integration
- [ ] Install Stripe .NET SDK
- [ ] Add Stripe configuration to appsettings
- [ ] Create `IPaymentService` interface
- [ ] Implement basic `StripePaymentService`
  - Create payment intent
  - Confirm payment
  - Handle webhooks (payment succeeded/failed)

### 3. Payment Entities and DTOs
```csharp
// Add to CreateRentalCommand
public decimal DailyRate { get; set; }
public int RentalDays { get; set; }

// New RentalPayment entity
public class RentalPayment : BaseEntity
{
    public int RentalId { get; set; }
    public string StripePaymentIntentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public Rental Rental { get; set; }
}

// Payment status enum
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
```

### 4. Commission Service
```csharp
public interface ICommissionService
{
    decimal CalculateCommission(decimal totalAmount);
    decimal CalculateOwnerEarnings(decimal totalAmount);
}

public class CommissionService : ICommissionService
{
    private const decimal COMMISSION_RATE = 0.10m; // 10%
    
    public decimal CalculateCommission(decimal totalAmount)
        => totalAmount * COMMISSION_RATE;
        
    public decimal CalculateOwnerEarnings(decimal totalAmount)
        => totalAmount - CalculateCommission(totalAmount);
}
```

## Frontend Implementation

### 1. Tool Creation/Editing
- [ ] Add daily rate input field to tool forms
- [ ] Add "Available for rent" checkbox
- [ ] Validate pricing inputs

### 2. Rental Request Flow
- [ ] Update rental request to show:
  - Daily rate
  - Number of days
  - Total cost
  - Platform fee (commission)
  - Owner earnings
- [ ] Add basic payment form with Stripe Elements
- [ ] Handle payment confirmation

### 3. Basic Dashboards
- [ ] **Owner Dashboard**: 
  - Total earnings
  - Pending payouts
  - Recent transactions
- [ ] **Admin Dashboard**:
  - Platform revenue
  - Commission collected
  - Payment success rate

## API Endpoints

### New/Updated Endpoints
```csharp
// Update existing
PUT /api/tools/{id} // Add pricing fields
POST /api/rentals   // Include payment processing

// New endpoints  
POST /api/payments/create-intent
POST /api/payments/confirm
POST /api/payments/webhook
GET /api/dashboard/owner-earnings
GET /api/dashboard/platform-revenue
```

## Configuration

### Stripe Setup
```json
// appsettings.json
{
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "Commission": {
    "Rate": 0.10
  }
}
```

### Environment Variables
```bash
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

## Implementation Steps

### Phase 1: Database and Backend (1 week)
- [ ] Create database migration for pricing fields
- [ ] Implement commission calculation service
- [ ] Add basic Stripe payment service
- [ ] Update rental creation logic

### Phase 2: Payment Processing (1 week)
- [ ] Implement payment intent creation
- [ ] Add webhook handling for payment events
- [ ] Update rental status based on payments
- [ ] Add payment failure handling

### Phase 3: Frontend Integration (1 week)
- [ ] Add pricing fields to tool management
- [ ] Implement rental payment flow
- [ ] Add Stripe Elements for card input
- [ ] Handle payment success/failure states

### Phase 4: Basic Reporting (0.5 weeks)
- [ ] Create owner earnings dashboard
- [ ] Add basic admin revenue overview
- [ ] Implement transaction history

### Phase 5: Testing and Polish (0.5 weeks)
- [ ] Test payment flows end-to-end
- [ ] Test webhook handling
- [ ] Error handling and edge cases
- [ ] Security review

**Total Timeline: 4 weeks**

## Payment Flow

### 1. Tool Owner Sets Pricing
```
Tool Creation/Edit → Set Daily Rate → Save Tool
```

### 2. Rental Request with Payment
```
Browse Tools → Request Rental → See Pricing Breakdown → 
Enter Payment Details → Confirm Payment → Rental Approved
```

### 3. Commission Handling
```
Payment Successful → Calculate Commission (10%) → 
Update Owner Earnings → Platform Keeps Commission
```

## Testing Strategy

### Unit Tests
- [ ] Commission calculation logic
- [ ] Payment amount calculations
- [ ] Rental pricing validation

### Integration Tests
- [ ] Stripe payment processing
- [ ] Webhook handling
- [ ] End-to-end rental flow

### Manual Testing
- [ ] Test with Stripe test cards
- [ ] Verify commission calculations
- [ ] Test payment failure scenarios

## Security Considerations

### Payment Security
- [ ] Never store card details (use Stripe tokens)
- [ ] Validate webhook signatures
- [ ] Secure API key storage
- [ ] HTTPS enforced for all payment pages

### Business Logic
- [ ] Validate pricing inputs (positive numbers)
- [ ] Prevent negative commissions
- [ ] Rate limiting on payment endpoints
- [ ] Audit logging for financial transactions

## Success Criteria

### Functional Requirements
- [ ] Tool owners can set rental prices
- [ ] Renters can pay for rentals securely
- [ ] Platform collects 10% commission automatically
- [ ] Payment failures are handled gracefully
- [ ] Basic financial reporting works

### Performance Requirements
- [ ] Payment processing < 5 seconds
- [ ] Dashboard loads < 2 seconds
- [ ] 99% payment success rate (excluding card declines)

### Business Requirements
- [ ] Commission accurately calculated and collected
- [ ] Owner earnings properly tracked
- [ ] Revenue reporting available for admin

## Limitations (By Design)

### Manual Processes
- **Payouts**: Manual transfer to tool owners (for now)
- **Refunds**: Manual processing through Stripe dashboard
- **Disputes**: Manual resolution process

### Single Payment Provider
- **Stripe Only**: No fallback payment methods
- **USD Only**: Single currency support
- **Cards Only**: No PayPal, bank transfers, etc.

### Basic Reporting
- **Simple Metrics**: Total revenue, earnings, transaction count
- **No Analytics**: No trends, forecasting, or advanced insights
- **No Exports**: Manual data extraction only

## Migration from Current System

### Data Migration
- [ ] Add default pricing to existing tools ($0 = free)
- [ ] Migrate existing rentals to new payment status
- [ ] Preserve rental history and relationships

### User Communication
- [ ] Notify tool owners about pricing features
- [ ] Update terms of service for commissions
- [ ] Provide pricing guidance and examples

## Future Extensions (Not in MVP)

This basic system enables future expansion to:
- Multiple payment providers (PayPal, Square)
- Dynamic commission rates by user tier
- Automated payout scheduling  
- Subscription tiers with reduced commissions
- Advanced analytics and reporting
- Mobile payment integration
- International payment support

## Dependencies

### External Services
- **Stripe Account**: Test and production accounts
- **SSL Certificate**: Required for payment processing
- **Webhook Endpoint**: Publicly accessible for Stripe callbacks

### Development Requirements
- **Stripe CLI**: For local webhook testing
- **Test Cards**: Stripe test card numbers
- **HTTPS Local Development**: ngrok or similar for webhook testing

---

**Note**: This MVP focuses on core payment functionality only. Once this foundation is working, it can be extended with the features outlined in `TODO_MONETIZATION_PLATFORM.md`.