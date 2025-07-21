namespace ToolsSharing.Core.Configuration;

public class PaymentConfiguration
{
    public decimal DefaultCommissionRate { get; set; } = 0.10m; // 10% default
    public decimal MinimumPayoutAmount { get; set; } = 10.00m;
    public string DefaultCurrency { get; set; } = "USD";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5000";
    public PayPalConfiguration PayPal { get; set; } = new();
}

public class PayPalConfiguration  
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Mode { get; set; } = "sandbox"; // "sandbox" or "live"
    public string WebhookId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    
    public string BaseUrl => Mode == "live" 
        ? "https://api-m.paypal.com" 
        : "https://api-m.sandbox.paypal.com";
}