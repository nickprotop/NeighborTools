using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolsSharing.Core.Configuration;
using ToolsSharing.Core.DTOs.Payment;
using ToolsSharing.Core.Interfaces;

namespace ToolsSharing.Infrastructure.PaymentProviders;

public class PayPalPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly PayPalConfiguration _config;
    private readonly ILogger<PayPalPaymentProvider> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public string Name => "PayPal";
    public bool IsConfigured => !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ClientSecret);

    public PayPalPaymentProvider(
        HttpClient httpClient,
        IOptions<PaymentConfiguration> paymentOptions,
        ILogger<PayPalPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _config = paymentOptions.Value.PayPal;
        _logger = logger;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        var authUrl = $"{_config.BaseUrl}/v1/oauth2/token";
        _logger.LogDebug("PayPal auth request to: {Url}", authUrl);
        
        var authRequest = new HttpRequestMessage(HttpMethod.Post, authUrl);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
        authRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        authRequest.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
        
        _logger.LogDebug("PayPal auth with ClientId: {ClientId} (Mode: {Mode})", 
            _config.ClientId?.Substring(0, Math.Min(8, _config.ClientId.Length)) + "...", _config.Mode);

        var response = await _httpClient.SendAsync(authRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal authentication failed. Status: {StatusCode}, Response: {Response}", 
                response.StatusCode, responseContent);
            throw new InvalidOperationException($"PayPal authentication failed: {response.StatusCode} - {responseContent}");
        }
        _logger.LogDebug("PayPal auth response: {Response}", responseContent);
        var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(responseContent);
        
        _accessToken = tokenResponse?.AccessToken ?? throw new InvalidOperationException("Failed to obtain PayPal access token");
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Subtract 60 seconds for safety

        _logger.LogDebug("PayPal authentication successful. Token length: {Length}, expires at: {Expiry}", 
            _accessToken?.Length ?? 0, _tokenExpiry);
        return _accessToken;
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request)
    {
        try
        {
            _logger.LogDebug("Starting PayPal order creation for rental {RentalId}", request.RentalId);
            var accessToken = await GetAccessTokenAsync();
            _logger.LogDebug("Successfully obtained PayPal access token: {TokenPreview}...", 
                accessToken?.Substring(0, Math.Min(20, accessToken.Length)));
            
            var orderRequest = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = request.RentalId.ToString(),
                        description = request.Description,
                        amount = new
                        {
                            currency_code = request.Currency,
                            value = request.Amount.ToString("F2")
                        },
                        payee = request.IsMarketplacePayment && !string.IsNullOrEmpty(request.PayeeEmail) 
                            ? new { email_address = request.PayeeEmail }
                            : null,
                        payment_instruction = request.IsMarketplacePayment && request.PlatformFee.HasValue
                            ? new
                            {
                                platform_fees = new[]
                                {
                                    new
                                    {
                                        amount = new
                                        {
                                            currency_code = request.Currency,
                                            value = request.PlatformFee.Value.ToString("F2")
                                        }
                                    }
                                }
                            }
                            : null
                    }
                },
                application_context = new
                {
                    return_url = request.ReturnUrl,
                    cancel_url = request.CancelUrl,
                    shipping_preference = "NO_SHIPPING",
                    user_action = "PAY_NOW"
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v2/checkout/orders");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");
            
            _logger.LogDebug("PayPal order request URL: {Url}", $"{_config.BaseUrl}/v2/checkout/orders");
            _logger.LogDebug("PayPal Bearer token length: {Length}", accessToken?.Length ?? 0);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal order creation failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                _logger.LogError("Request headers: {Headers}", string.Join(", ", httpRequest.Headers.Select(h => $"{h.Key}: {string.Join(",", h.Value)}")));
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal order creation failed: {response.StatusCode} - {responseContent}"
                };
            }

            _logger.LogDebug("PayPal order response: {Response}", responseContent);
            var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent);
            _logger.LogDebug("Deserialized order ID: {OrderId}, Links count: {LinksCount}", 
                orderResponse?.Id, orderResponse?.Links?.Count ?? 0);
            
            var approvalUrl = orderResponse?.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;
            _logger.LogDebug("Found approval URL: {ApprovalUrl}", approvalUrl);

            return new CreatePaymentResult
            {
                Success = true,
                PaymentId = orderResponse?.Id,
                OrderId = orderResponse?.Id,
                ApprovalUrl = approvalUrl,
                AdditionalData = { ["status"] = orderResponse?.Status ?? "" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal payment");
            return new CreatePaymentResult
            {
                Success = false,
                ErrorMessage = $"Error creating payment: {ex.Message}"
            };
        }
    }

    public async Task<CapturePaymentResult> CapturePaymentAsync(CapturePaymentRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v2/checkout/orders/{request.OrderId}/capture");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal capture failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return new CapturePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal capture failed: {response.StatusCode}"
                };
            }

            var captureResponse = JsonSerializer.Deserialize<PayPalCaptureResponse>(responseContent);
            var capture = captureResponse?.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault();

            return new CapturePaymentResult
            {
                Success = true,
                TransactionId = capture?.Id,
                CapturedAmount = decimal.Parse(capture?.Amount?.Value ?? "0"),
                Status = capture?.Status,
                AdditionalData = { ["order_status"] = captureResponse?.Status ?? "" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment");
            return new CapturePaymentResult
            {
                Success = false,
                ErrorMessage = $"Error capturing payment: {ex.Message}"
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_config.BaseUrl}/v2/checkout/orders/{paymentId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to get payment status: {response.StatusCode}"
                };
            }

            var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(responseContent);
            
            return new PaymentStatusResult
            {
                Success = true,
                PaymentId = orderResponse?.Id ?? paymentId,
                Status = orderResponse?.Status ?? "UNKNOWN",
                Amount = decimal.Parse(orderResponse?.PurchaseUnits?.FirstOrDefault()?.Amount?.Value ?? "0"),
                Currency = orderResponse?.PurchaseUnits?.FirstOrDefault()?.Amount?.CurrencyCode ?? "USD",
                CreatedAt = orderResponse?.CreateTime,
                UpdatedAt = orderResponse?.UpdateTime,
                PayerEmail = orderResponse?.Payer?.Email,
                PayerName = $"{orderResponse?.Payer?.Name?.GivenName} {orderResponse?.Payer?.Name?.Surname}".Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal payment status");
            return new PaymentStatusResult
            {
                Success = false,
                ErrorMessage = $"Error getting payment status: {ex.Message}"
            };
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(RefundPaymentRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var refundRequest = new
            {
                amount = new
                {
                    value = request.Amount.ToString("F2"),
                    currency_code = "USD"
                },
                note_to_payer = request.Reason
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v2/payments/captures/{request.PaymentId}/refund");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(refundRequest), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal refund failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal refund failed: {response.StatusCode}"
                };
            }

            var refundResponse = JsonSerializer.Deserialize<PayPalRefundResponse>(responseContent);

            return new RefundResult
            {
                Success = true,
                RefundId = refundResponse?.Id,
                RefundedAmount = decimal.Parse(refundResponse?.Amount?.Value ?? "0"),
                Status = refundResponse?.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal refund");
            return new RefundResult
            {
                Success = false,
                ErrorMessage = $"Error processing refund: {ex.Message}"
            };
        }
    }

    public async Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var payoutRequest = new
            {
                sender_batch_header = new
                {
                    sender_batch_id = request.BatchId ?? Guid.NewGuid().ToString(),
                    email_subject = "You have received a payment from NeighborTools",
                    email_message = request.Note
                },
                items = new[]
                {
                    new
                    {
                        recipient_type = "EMAIL",
                        amount = new
                        {
                            value = request.Amount.ToString("F2"),
                            currency = request.Currency
                        },
                        receiver = request.RecipientEmail,
                        note = request.Note,
                        sender_item_id = request.RecipientId
                    }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v1/payments/payouts");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payoutRequest), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal payout failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return new CreatePayoutResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal payout failed: {response.StatusCode}"
                };
            }

            var payoutResponse = JsonSerializer.Deserialize<PayPalPayoutResponse>(responseContent);

            return new CreatePayoutResult
            {
                Success = true,
                BatchId = payoutResponse?.BatchHeader?.PayoutBatchId,
                Status = payoutResponse?.BatchHeader?.BatchStatus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal payout");
            return new CreatePayoutResult
            {
                Success = false,
                ErrorMessage = $"Error creating payout: {ex.Message}"
            };
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(string payoutId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_config.BaseUrl}/v1/payments/payouts/{payoutId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PayoutStatusResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to get payout status: {response.StatusCode}"
                };
            }

            var payoutResponse = JsonSerializer.Deserialize<PayPalPayoutBatchResponse>(responseContent);
            var item = payoutResponse?.Items?.FirstOrDefault();

            return new PayoutStatusResult
            {
                Success = true,
                PayoutId = item?.PayoutItemId ?? payoutId,
                Status = item?.TransactionStatus ?? "UNKNOWN",
                Amount = decimal.Parse(item?.PayoutItem?.Amount?.Value ?? "0"),
                Currency = item?.PayoutItem?.Amount?.CurrencyCode ?? "USD",
                ProcessedAt = item?.TimeProcessed,
                RecipientEmail = item?.PayoutItem?.Receiver
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal payout status");
            return new PayoutStatusResult
            {
                Success = false,
                ErrorMessage = $"Error getting payout status: {ex.Message}"
            };
        }
    }

    public async Task<WebhookValidationResult> ValidateWebhookAsync(string payload, Dictionary<string, string> headers)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            var verificationRequest = new
            {
                auth_algo = headers.GetValueOrDefault("PAYPAL-AUTH-ALGO"),
                cert_url = headers.GetValueOrDefault("PAYPAL-CERT-URL"),
                transmission_id = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-ID"),
                transmission_sig = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-SIG"),
                transmission_time = headers.GetValueOrDefault("PAYPAL-TRANSMISSION-TIME"),
                webhook_id = _config.WebhookId,
                webhook_event = JsonSerializer.Deserialize<object>(payload)
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/v1/notifications/verify-webhook-signature");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(verificationRequest), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new WebhookValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Webhook validation failed: {response.StatusCode}"
                };
            }

            var verificationResponse = JsonSerializer.Deserialize<PayPalWebhookVerificationResponse>(responseContent);
            var webhookEvent = JsonSerializer.Deserialize<PayPalWebhookEvent>(payload);

            return new WebhookValidationResult
            {
                IsValid = verificationResponse?.VerificationStatus == "SUCCESS",
                EventType = webhookEvent?.EventType,
                EventId = webhookEvent?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayPal webhook");
            return new WebhookValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error validating webhook: {ex.Message}"
            };
        }
    }

    public async Task<WebhookProcessResult> ProcessWebhookAsync(string payload)
    {
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<PayPalWebhookEvent>(payload);
            
            return new WebhookProcessResult
            {
                Success = true,
                EventType = webhookEvent?.EventType ?? "UNKNOWN",
                ResourceId = webhookEvent?.Resource?.Id,
                ProcessedData = 
                {
                    ["resource_type"] = webhookEvent?.ResourceType ?? "",
                    ["summary"] = webhookEvent?.Summary ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return new WebhookProcessResult
            {
                Success = false,
                ErrorMessage = $"Error processing webhook: {ex.Message}"
            };
        }
    }

    // PayPal API response models
    private class PayPalTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class PayPalOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("create_time")]
        public DateTime? CreateTime { get; set; }
        
        [JsonPropertyName("update_time")]
        public DateTime? UpdateTime { get; set; }
        
        [JsonPropertyName("links")]
        public List<PayPalLink> Links { get; set; } = new();
        
        [JsonPropertyName("purchase_units")]
        public List<PayPalPurchaseUnit> PurchaseUnits { get; set; } = new();
        
        [JsonPropertyName("payer")]
        public PayPalPayer? Payer { get; set; }
    }

    private class PayPalLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;
        
        [JsonPropertyName("rel")]
        public string Rel { get; set; } = string.Empty;
        
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
    }

    private class PayPalPurchaseUnit
    {
        public PayPalAmount Amount { get; set; } = new();
        public PayPalPayments? Payments { get; set; }
    }

    private class PayPalAmount
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class PayPalPayments
    {
        public List<PayPalCapture> Captures { get; set; } = new();
    }

    private class PayPalCapture
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PayPalAmount Amount { get; set; } = new();
    }

    private class PayPalCaptureResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<PayPalPurchaseUnit> PurchaseUnits { get; set; } = new();
    }

    private class PayPalPayer
    {
        public string? Email { get; set; }
        public PayPalName? Name { get; set; }
    }

    private class PayPalName
    {
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
    }

    private class PayPalRefundResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PayPalAmount Amount { get; set; } = new();
    }

    private class PayPalPayoutResponse
    {
        public PayPalBatchHeader BatchHeader { get; set; } = new();
    }

    private class PayPalBatchHeader
    {
        public string PayoutBatchId { get; set; } = string.Empty;
        public string BatchStatus { get; set; } = string.Empty;
    }

    private class PayPalPayoutBatchResponse
    {
        public List<PayPalPayoutItem> Items { get; set; } = new();
    }

    private class PayPalPayoutItem
    {
        public string PayoutItemId { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public DateTime? TimeProcessed { get; set; }
        public PayPalPayoutItemDetail PayoutItem { get; set; } = new();
    }

    private class PayPalPayoutItemDetail
    {
        public string Receiver { get; set; } = string.Empty;
        public PayPalAmount Amount { get; set; } = new();
    }

    private class PayPalWebhookVerificationResponse
    {
        public string VerificationStatus { get; set; } = string.Empty;
    }

    private class PayPalWebhookEvent
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public PayPalWebhookResource? Resource { get; set; }
    }

    private class PayPalWebhookResource
    {
        public string Id { get; set; } = string.Empty;
    }
}