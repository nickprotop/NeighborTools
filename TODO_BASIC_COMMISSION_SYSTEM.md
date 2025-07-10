# TODO: Enhanced Basic Commission System (Robust MVP)

## Overview
Implement a robust, future-proof commission system with dual payment provider support (Stripe + PayPal). This maintains MVP scope while building enterprise-grade foundations for future expansion.

## Scope (Enhanced MVP)
- Simple rental pricing with commission calculation
- Fixed commission rate (10%) with flexible configuration
- **Dual payment provider support**: Stripe + PayPal
- Manual payout process (automated-ready architecture)
- Essential reporting with audit trails
- Future-proof extensible architecture

## Out of Scope (Future TODOs)
- Additional payment providers beyond Stripe/PayPal
- Dynamic commission rates and tier system
- Automated payout scheduling
- Subscription tiers
- Advanced analytics and ML features
- Tax integration
- Mobile payment apps (Apple Pay, Google Pay)

## Enhanced Database Changes

### Core Table Enhancements
```sql
-- Enhanced Rentals table
ALTER TABLE Rentals ADD COLUMN DailyRate DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN CommissionAmount DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN OwnerEarnings DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Rentals ADD COLUMN Currency VARCHAR(3) NOT NULL DEFAULT 'USD';
ALTER TABLE Rentals ADD COLUMN PaymentStatus ENUM('pending', 'completed', 'failed', 'refunded') NOT NULL DEFAULT 'pending';

-- Enhanced Tools table
ALTER TABLE Tools ADD COLUMN DailyRate DECIMAL(10,2) NOT NULL DEFAULT 0;
ALTER TABLE Tools ADD COLUMN Currency VARCHAR(3) NOT NULL DEFAULT 'USD';
ALTER TABLE Tools ADD COLUMN IsAvailableForRent BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE Tools ADD COLUMN MinRentalDays INT NOT NULL DEFAULT 1;
ALTER TABLE Tools ADD COLUMN MaxRentalDays INT NULL;

-- Multi-provider payment transactions
CREATE TABLE PaymentTransactions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    RentalId INT NOT NULL,
    PaymentProvider ENUM('stripe', 'paypal') NOT NULL,
    ProviderTransactionId VARCHAR(255) NOT NULL,
    PaymentIntentId VARCHAR(255) NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    Status ENUM('pending', 'processing', 'completed', 'failed', 'refunded', 'disputed') NOT NULL,
    FailureReason VARCHAR(500) NULL,
    ProcessedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    FOREIGN KEY (RentalId) REFERENCES Rentals(Id),
    INDEX idx_provider_transaction (PaymentProvider, ProviderTransactionId),
    INDEX idx_rental_status (RentalId, Status)
);

-- Future-proof commission configuration
CREATE TABLE CommissionSettings (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    CommissionRate DECIMAL(5,4) NOT NULL, -- Supports up to 99.99%
    EffectiveFromDate DATETIME NOT NULL,
    EffectiveToDate DATETIME NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL,
    INDEX idx_effective_dates (EffectiveFromDate, EffectiveToDate, IsActive)
);

-- Insert default commission rate
INSERT INTO CommissionSettings (Name, CommissionRate, EffectiveFromDate, IsActive, CreatedAt) 
VALUES ('Default Platform Commission', 0.1000, NOW(), TRUE, NOW());

-- Payment provider settings (extensible)
CREATE TABLE PaymentProviderSettings (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Provider ENUM('stripe', 'paypal') NOT NULL,
    IsEnabled BOOLEAN NOT NULL DEFAULT TRUE,
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    Priority INT NOT NULL DEFAULT 1,
    SupportedCurrencies JSON NOT NULL,
    ConfigurationData JSON NOT NULL, -- Encrypted sensitive data
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    UNIQUE KEY unique_default (IsDefault, IsEnabled)
);

-- Comprehensive audit trail
CREATE TABLE FinancialAuditLog (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    EntityType VARCHAR(50) NOT NULL, -- 'rental', 'payment', 'commission'
    EntityId INT NOT NULL,
    Action VARCHAR(50) NOT NULL, -- 'created', 'updated', 'paid', 'refunded'
    OldValues JSON NULL,
    NewValues JSON NOT NULL,
    UserId INT NULL,
    IPAddress VARCHAR(45) NULL,
    UserAgent VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    INDEX idx_entity (EntityType, EntityId),
    INDEX idx_action_date (Action, CreatedAt)
);
```

## Robust Backend Implementation

### 1. Future-Proof Payment Architecture
```csharp
// Clean abstraction for multiple payment providers
public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentResult> CreatePaymentIntentAsync(PaymentRequest request);
    Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId);
    Task<RefundResult> ProcessRefundAsync(RefundRequest request);
    Task<bool> ValidateWebhookAsync(string payload, string signature);
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
    bool SupportsRecurring { get; }
    List<string> SupportedCurrencies { get; }
}

public interface IPaymentProviderFactory
{
    IPaymentProvider GetProvider(PaymentProviderType type);
    Task<IPaymentProvider> GetDefaultProviderAsync();
    Task<List<IPaymentProvider>> GetEnabledProvidersAsync();
}

// Enhanced commission service
public interface ICommissionService
{
    Task<CommissionCalculation> CalculateCommissionAsync(decimal amount, DateTime effectiveDate);
    Task<decimal> GetCurrentCommissionRateAsync();
    Task<CommissionBreakdown> GetCommissionBreakdownAsync(int rentalId);
}

// Comprehensive payment service
public interface IPaymentService
{
    Task<PaymentResult> ProcessRentalPaymentAsync(ProcessPaymentRequest request);
    Task<RefundResult> ProcessRefundAsync(RefundRequest request);
    Task<PaymentStatus> GetPaymentStatusAsync(int rentalId);
    Task<List<PaymentTransaction>> GetPaymentHistoryAsync(int userId);
}
```

### 2. Stripe Payment Provider Implementation
```csharp
public class StripePaymentProvider : IPaymentProvider
{
    public string ProviderName => "Stripe";
    public bool SupportsRecurring => true;
    public List<string> SupportedCurrencies => new() { "USD", "EUR", "GBP", "CAD", "AUD" };

    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentProvider> _logger;

    public async Task<PaymentResult> CreatePaymentIntentAsync(PaymentRequest request)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100), // Convert to cents
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            Metadata = request.Metadata,
            PaymentMethodTypes = new List<string> { "card" },
            CaptureMethod = "automatic",
            ConfirmationMethod = "manual",
            Confirm = false
        };

        var paymentIntent = await _paymentIntentService.CreateAsync(options);

        return new PaymentResult
        {
            Success = true,
            TransactionId = paymentIntent.Id,
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = MapStripeStatus(paymentIntent.Status),
            Amount = request.Amount,
            Currency = request.Currency
        };
    }

    public async Task<bool> ValidateWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
            return await ProcessStripeEventAsync(stripeEvent);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return false;
        }
    }
}
```

### 3. PayPal Payment Provider Implementation
```csharp
public class PayPalPaymentProvider : IPaymentProvider
{
    public string ProviderName => "PayPal";
    public bool SupportsRecurring => true;
    public List<string> SupportedCurrencies => new() { "USD", "EUR", "GBP", "CAD", "AUD", "JPY" };

    private readonly PayPalHttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalPaymentProvider> _logger;

    public async Task<PaymentResult> CreatePaymentIntentAsync(PaymentRequest request)
    {
        var orderRequest = new OrdersCreateRequest();
        orderRequest.Prefer("return=representation");
        orderRequest.RequestBody(new OrderRequest()
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>()
            {
                new PurchaseUnitRequest()
                {
                    AmountWithBreakdown = new AmountWithBreakdown()
                    {
                        CurrencyCode = request.Currency,
                        Value = request.Amount.ToString("F2")
                    },
                    Description = request.Description
                }
            },
            ApplicationContext = new ApplicationContext()
            {
                ReturnUrl = $"{_baseUrl}/api/payments/paypal/return",
                CancelUrl = $"{_baseUrl}/api/payments/paypal/cancel"
            }
        });

        var response = await _client.Execute(orderRequest);
        var order = response.Result<Order>();

        return new PaymentResult
        {
            Success = true,
            TransactionId = order.Id,
            PaymentIntentId = order.Id,
            ClientSecret = order.Id,
            Status = MapPayPalStatus(order.Status),
            Amount = request.Amount,
            Currency = request.Currency,
            RedirectUrl = order.Links.FirstOrDefault(l => l.Rel == "approve")?.Href
        };
    }

    public async Task<bool> ValidateWebhookAsync(string payload, string signature)
    {
        // PayPal webhook validation implementation
        return await ValidatePayPalWebhookAsync(payload, signature);
    }
}
```

### 4. Enhanced Commission Service
```csharp
public class CommissionService : ICommissionService
{
    private readonly ICommissionSettingsRepository _settingsRepository;
    private readonly ILogger<CommissionService> _logger;

    public async Task<CommissionCalculation> CalculateCommissionAsync(decimal amount, DateTime effectiveDate)
    {
        var rate = await GetCommissionRateForDateAsync(effectiveDate);
        var commissionAmount = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
        var ownerEarnings = amount - commissionAmount;

        return new CommissionCalculation
        {
            TotalAmount = amount,
            CommissionRate = rate,
            CommissionAmount = commissionAmount,
            OwnerEarnings = ownerEarnings,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private async Task<decimal> GetCommissionRateForDateAsync(DateTime effectiveDate)
    {
        var setting = await _settingsRepository.GetActiveSettingAsync(effectiveDate);
        return setting?.CommissionRate ?? 0.10m; // Default 10%
    }
}
```

## Enhanced Frontend Implementation

### 1. Payment Provider Selection
```typescript
// Payment method selector component
export const PaymentMethodSelector: React.FC<PaymentMethodSelectorProps> = ({
  amount,
  currency,
  onProviderSelect
}) => {
  const { data: providers } = usePaymentProviders();
  const [selectedProvider, setSelectedProvider] = useState<PaymentProvider>();

  return (
    <div className="payment-methods">
      <h3>Choose Payment Method</h3>
      
      {providers?.map(provider => (
        <PaymentMethodCard
          key={provider.id}
          provider={provider}
          selected={selectedProvider?.id === provider.id}
          amount={amount}
          currency={currency}
          onClick={() => setSelectedProvider(provider)}
          features={provider.features}
        />
      ))}
      
      <SecurityBadges />
    </div>
  );
};
```

### 2. Enhanced Rental Payment Flow
```typescript
export const RentalPaymentFlow: React.FC<RentalPaymentFlowProps> = ({
  rental,
  onSuccess,
  onError
}) => {
  const [paymentMethod, setPaymentMethod] = useState<PaymentProvider>();
  const [pricing, setPricing] = useState<PricingBreakdown>();
  const [processing, setProcessing] = useState(false);

  const calculatePricing = useCallback(async () => {
    const result = await api.calculateRentalPricing({
      toolId: rental.toolId,
      startDate: rental.startDate,
      endDate: rental.endDate
    });
    setPricing(result);
  }, [rental]);

  return (
    <div className="rental-payment-flow">
      <RentalSummary rental={rental} />
      
      {pricing && (
        <PricingBreakdown
          dailyRate={pricing.dailyRate}
          days={pricing.days}
          subtotal={pricing.subtotal}
          platformFee={pricing.platformFee}
          total={pricing.totalAmount}
          currency={pricing.currency}
        />
      )}

      <PaymentMethodSelector
        amount={pricing?.totalAmount}
        currency={pricing?.currency}
        onProviderSelect={setPaymentMethod}
      />

      {paymentMethod?.type === 'stripe' && (
        <StripePaymentForm
          clientSecret={pricing?.clientSecret}
          onSuccess={handlePayment}
        />
      )}

      {paymentMethod?.type === 'paypal' && (
        <PayPalPaymentForm
          orderId={pricing?.paypalOrderId}
          onSuccess={handlePayment}
        />
      )}

      <PaymentSecurityInfo />
    </div>
  );
};
```

### 3. Stripe Integration
```typescript
export const StripePaymentForm: React.FC<StripePaymentFormProps> = ({
  clientSecret,
  onSuccess
}) => {
  const stripe = useStripe();
  const elements = useElements();
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!stripe || !elements) return;

    setProcessing(true);

    const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret, {
      payment_method: {
        card: elements.getElement(CardElement)!,
      }
    });

    setProcessing(false);

    if (error) {
      onError(error);
    } else if (paymentIntent?.status === 'succeeded') {
      onSuccess(paymentIntent);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="stripe-payment-form">
      <CardElement options={cardElementOptions} />
      <button type="submit" disabled={!stripe || processing}>
        {processing ? 'Processing...' : 'Pay with Card'}
      </button>
    </form>
  );
};
```

### 4. PayPal Integration
```typescript
export const PayPalPaymentForm: React.FC<PayPalPaymentFormProps> = ({
  orderId,
  onSuccess
}) => {
  return (
    <PayPalScriptProvider options={{ 
      "client-id": process.env.REACT_APP_PAYPAL_CLIENT_ID!,
      currency: "USD"
    }}>
      <PayPalButtons
        createOrder={() => Promise.resolve(orderId)}
        onApprove={async (data, actions) => {
          const order = await actions.order!.capture();
          onSuccess(order);
        }}
        onError={(err) => {
          console.error('PayPal payment error:', err);
        }}
        style={{
          layout: 'vertical',
          color: 'blue',
          shape: 'rect',
          label: 'pay'
        }}
      />
    </PayPalScriptProvider>
  );
};
```

## API Endpoints

### Enhanced/New Endpoints
```csharp
// Enhanced existing endpoints
PUT /api/tools/{id}           // Add pricing fields
POST /api/rentals             // Include payment processing

// New payment endpoints
GET /api/payments/providers   // Get available payment providers
POST /api/payments/intent     // Create payment intent
POST /api/payments/confirm    // Confirm payment
POST /api/payments/webhook/stripe    // Stripe webhook
POST /api/payments/webhook/paypal    // PayPal webhook
GET /api/payments/status/{rentalId}  // Get payment status

// Dashboard endpoints
GET /api/dashboard/owner-earnings    // Owner financial dashboard
GET /api/dashboard/platform-revenue  // Admin revenue overview
GET /api/dashboard/payment-analytics // Payment success metrics
```

## Configuration

### Enhanced Configuration Structure
```json
{
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "ApiVersion": "2022-11-15"
  },
  "PayPal": {
    "ClientId": "paypal_client_id",
    "ClientSecret": "paypal_client_secret",
    "WebhookId": "webhook_id",
    "Environment": "sandbox"
  },
  "Commission": {
    "DefaultRate": 0.10,
    "MinimumRate": 0.05,
    "MaximumRate": 0.25
  },
  "Payment": {
    "DefaultProvider": "stripe",
    "SupportedCurrencies": ["USD", "EUR", "GBP", "CAD"],
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30
  }
}
```

### Environment Variables
```bash
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
PAYPAL_CLIENT_ID=paypal_client_id
PAYPAL_CLIENT_SECRET=paypal_client_secret
PAYPAL_WEBHOOK_ID=webhook_id
```

## Implementation Timeline (6 Weeks)

### Week 1: Foundation & Database
- [ ] Enhanced database schema with future-proof design
- [ ] Payment provider abstraction layer implementation
- [ ] Basic commission service with configurable rates
- [ ] Comprehensive audit logging system
- [ ] Database migration scripts

### Week 2: Stripe Implementation
- [ ] Complete Stripe payment provider implementation
- [ ] Stripe webhook handling for all payment events
- [ ] Frontend Stripe Elements integration
- [ ] Comprehensive testing with Stripe test environment
- [ ] Error handling and retry logic

### Week 3: PayPal Implementation
- [ ] PayPal payment provider implementation
- [ ] PayPal webhook integration and validation
- [ ] Frontend PayPal payment flow
- [ ] Cross-provider testing and validation
- [ ] Provider failover testing

### Week 4: Frontend Enhancement
- [ ] Payment method selection UI components
- [ ] Enhanced rental payment flow with provider choice
- [ ] Detailed pricing breakdown components
- [ ] Comprehensive error handling and user feedback
- [ ] Mobile-responsive payment forms

### Week 5: Integration & Testing
- [ ] End-to-end payment flow testing (both providers)
- [ ] Commission calculation verification
- [ ] Security testing and validation
- [ ] Performance testing under load
- [ ] User acceptance testing

### Week 6: Production Preparation
- [ ] Security hardening and final review
- [ ] Performance optimization
- [ ] Documentation completion
- [ ] Production deployment scripts
- [ ] Monitoring and alerting setup

## Payment Flow

### 1. Tool Owner Sets Pricing
```
Tool Creation/Edit → Set Daily Rate & Currency → 
Configure Availability → Save Tool → Tool Available for Rent
```

### 2. Enhanced Rental Request with Payment Choice
```
Browse Tools → View Pricing → Request Rental → 
See Detailed Pricing Breakdown → Choose Payment Method (Stripe/PayPal) → 
Enter Payment Details → Confirm Payment → Rental Approved
```

### 3. Commission Handling with Audit Trail
```
Payment Successful → Calculate Commission → Update Owner Earnings → 
Platform Keeps Commission → Log All Financial Transactions → 
Send Confirmation Notifications
```

## Testing Strategy

### Unit Tests
- [ ] Commission calculation logic with edge cases
- [ ] Payment provider implementations
- [ ] Currency conversion and rounding
- [ ] Validation logic for all financial inputs

### Integration Tests
- [ ] Stripe payment processing with test cards
- [ ] PayPal payment processing with sandbox
- [ ] Webhook handling for both providers
- [ ] Database transaction integrity
- [ ] Cross-provider payment flows

### End-to-End Tests
- [ ] Complete rental flow with Stripe payment
- [ ] Complete rental flow with PayPal payment
- [ ] Payment failure scenarios and recovery
- [ ] Commission calculation accuracy
- [ ] Audit trail completeness

### Security Tests
- [ ] Webhook signature validation
- [ ] Payment data encryption
- [ ] SQL injection prevention
- [ ] Rate limiting effectiveness
- [ ] Access control validation

## Security & Compliance

### Payment Security
- [ ] PCI DSS compliance through Stripe/PayPal (no card storage)
- [ ] Webhook signature validation for both providers
- [ ] Secure API key storage and rotation
- [ ] HTTPS enforcement for all payment endpoints
- [ ] Rate limiting on payment operations

### Data Protection
- [ ] Comprehensive audit logging for all financial operations
- [ ] Sensitive data encryption at rest and in transit
- [ ] Regular security scans and penetration testing
- [ ] GDPR compliance for user financial data
- [ ] Secure backup and recovery procedures

### Business Logic Security
- [ ] Input validation for all financial calculations
- [ ] Prevention of negative amounts and commission manipulation
- [ ] Idempotency protection for payment operations
- [ ] Transaction integrity with database constraints
- [ ] Fraud detection for suspicious patterns

## Success Criteria

### Functional Requirements
- [ ] Tool owners can set rental prices in supported currencies
- [ ] Users can choose between Stripe and PayPal for payments
- [ ] Platform collects 10% commission automatically and accurately
- [ ] Payment failures are handled gracefully with clear user messaging
- [ ] All financial transactions are logged with comprehensive audit trails
- [ ] Basic reporting shows earnings, commissions, and transaction history

### Performance Requirements
- [ ] Payment processing completes in < 5 seconds for both providers
- [ ] Dashboard loads in < 2 seconds with current data
- [ ] System supports 100+ concurrent payment operations
- [ ] 99.5% payment success rate (excluding legitimate declines)
- [ ] Database queries optimized for financial reporting

### Business Requirements
- [ ] Commission accurately calculated and collected for all transactions
- [ ] Owner earnings properly tracked and reportable
- [ ] Platform revenue reporting available for admin users
- [ ] Support for USD, EUR, GBP, CAD currencies
- [ ] Seamless user experience across both payment providers

## Future Extension Points

### Designed for Easy Extension
This robust basic system provides clean extension points for:

#### Additional Payment Providers
- Clean `IPaymentProvider` interface for adding new providers
- Database schema supports unlimited provider types
- Frontend components designed for dynamic provider lists

#### Advanced Commission Features
- Database ready for user-specific commission rates
- Service architecture supports complex calculation rules
- Audit system tracks all commission changes

#### Automated Financial Operations
- Repository patterns ready for automated payout scheduling
- Comprehensive transaction tracking enables automated reconciliation
- Audit trails support automated compliance reporting

#### Enhanced Analytics
- Detailed transaction logging enables advanced reporting
- Database design supports complex financial queries
- Service layer ready for ML integration

## Dependencies

### External Services Required
- **Stripe Account**: Test and production accounts with webhook endpoints
- **PayPal Developer Account**: Sandbox and live application credentials
- **SSL Certificate**: Required for payment processing security
- **Webhook Endpoints**: Publicly accessible endpoints for payment callbacks

### Development Tools
- **Stripe CLI**: For local webhook testing and event simulation
- **PayPal Developer Tools**: For testing PayPal integration locally
- **ngrok or similar**: For HTTPS local development and webhook testing
- **Postman/Insomnia**: For API testing and validation

### Third-Party NuGet Packages
```xml
<!-- Payment Processing -->
<PackageReference Include="Stripe.net" Version="43.0.0" />
<PackageReference Include="PayPalCheckoutSdk" Version="1.0.4" />

<!-- Security -->
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="9.0.0" />

<!-- Logging and Monitoring -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

### Frontend Dependencies
```json
{
  "@stripe/stripe-js": "^2.1.0",
  "@stripe/react-stripe-js": "^2.1.0",
  "@paypal/react-paypal-js": "^8.1.0",
  "react-hook-form": "^7.45.0",
  "yup": "^1.2.0"
}
```

## Migration Strategy

### From Current System
- [ ] Add default pricing ($0) to all existing tools
- [ ] Create initial commission settings record
- [ ] Migrate existing rental data to new payment status fields
- [ ] Preserve all existing relationships and history

### User Communication
- [ ] Email notification to tool owners about new pricing features
- [ ] In-app tutorials for setting up rental pricing
- [ ] Help documentation for both Stripe and PayPal setup
- [ ] Updated terms of service reflecting commission structure

### Rollback Plan
- [ ] Database migration rollback scripts
- [ ] Feature flag system for gradual rollout
- [ ] Monitoring alerts for payment processing issues
- [ ] Quick disable mechanism for problematic providers

---

**Total Timeline: 6 weeks**

**Key Benefits:**
- **Dual Payment Support**: Stripe + PayPal increases payment success rates
- **Future-Proof Architecture**: Clean abstractions enable easy expansion
- **Enterprise-Grade Security**: Comprehensive audit trails and security measures
- **Robust Error Handling**: Graceful failure modes and user feedback
- **Production Ready**: Built for scale with monitoring and observability

This enhanced plan delivers a production-ready commission system that maintains MVP simplicity while building the foundation for enterprise-scale growth.