using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ToolsSharing.Infrastructure.Security;

public interface IPayPalWebhookValidator
{
    Task<bool> ValidateWebhookSignatureAsync(string webhookBody, string authAlgo, string transmission_id, string cert_id, string transmission_time, string webhook_signature, string webhook_id);
    bool ValidateTimestamp(string transmissionTime, int toleranceMinutes = 5);
}

public class PayPalWebhookValidator : IPayPalWebhookValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalWebhookValidator> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _certCache = new();
    private readonly SemaphoreSlim _certCacheLock = new(1, 1);

    public PayPalWebhookValidator(HttpClient httpClient, ILogger<PayPalWebhookValidator> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> ValidateWebhookSignatureAsync(
        string webhookBody, 
        string authAlgo, 
        string transmissionId, 
        string certId, 
        string transmissionTime, 
        string webhookSignature, 
        string webhookId)
    {
        try
        {
            // Validate timestamp first (prevents replay attacks)
            if (!ValidateTimestamp(transmissionTime))
            {
                _logger.LogWarning("Webhook rejected: Invalid timestamp {TransmissionTime}", transmissionTime);
                return false;
            }

            // Get PayPal certificate
            var certificate = await GetPayPalCertificateAsync(certId);
            if (string.IsNullOrEmpty(certificate))
            {
                _logger.LogError("Failed to retrieve PayPal certificate for cert_id: {CertId}", certId);
                return false;
            }

            // Construct expected signature string
            var expectedSignatureString = $"{authAlgo}|{transmissionId}|{certId}|{transmissionTime}|{webhookId}|{Convert.ToBase64String(ComputeSHA256Hash(webhookBody))}";
            
            _logger.LogDebug("Expected signature string: {SignatureString}", expectedSignatureString);

            // Verify signature using PayPal's public key
            var isValid = VerifySignature(expectedSignatureString, webhookSignature, certificate);
            
            if (!isValid)
            {
                _logger.LogWarning("Webhook signature validation failed for transmission_id: {TransmissionId}", transmissionId);
            }
            else
            {
                _logger.LogInformation("Webhook signature validated successfully for transmission_id: {TransmissionId}", transmissionId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature for transmission_id: {TransmissionId}", transmissionId);
            return false;
        }
    }

    public bool ValidateTimestamp(string transmissionTime, int toleranceMinutes = 5)
    {
        try
        {
            if (!DateTime.TryParse(transmissionTime, null, DateTimeStyles.AdjustToUniversal, out var timestamp))
            {
                _logger.LogWarning("Invalid timestamp format: {TransmissionTime}", transmissionTime);
                return false;
            }

            var now = DateTime.UtcNow;
            var timeDifference = Math.Abs((now - timestamp).TotalMinutes);

            if (timeDifference > toleranceMinutes)
            {
                _logger.LogWarning("Timestamp too old: {TransmissionTime}, difference: {TimeDifference} minutes", 
                    transmissionTime, timeDifference);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating timestamp: {TransmissionTime}", transmissionTime);
            return false;
        }
    }

    private async Task<string?> GetPayPalCertificateAsync(string certId)
    {
        // Check cache first
        await _certCacheLock.WaitAsync();
        try
        {
            if (_certCache.TryGetValue(certId, out var cachedCert))
            {
                return cachedCert;
            }
        }
        finally
        {
            _certCacheLock.Release();
        }

        try
        {
            // Get PayPal environment configuration
            var paypalMode = _configuration["Payment:PayPal:Mode"] ?? "sandbox";
            var baseUrl = paypalMode == "live" 
                ? "https://api.paypal.com" 
                : "https://api.sandbox.paypal.com";

            // Fetch certificate from PayPal
            var response = await _httpClient.GetAsync($"{baseUrl}/v1/notifications/certs/{certId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch PayPal certificate. Status: {StatusCode}, CertId: {CertId}", 
                    response.StatusCode, certId);
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var certResponse = JsonSerializer.Deserialize<PayPalCertificateResponse>(jsonResponse);

            if (certResponse?.PublicKey == null)
            {
                _logger.LogError("Invalid certificate response from PayPal for cert_id: {CertId}", certId);
                return null;
            }

            // Cache the certificate
            await _certCacheLock.WaitAsync();
            try
            {
                _certCache[certId] = certResponse.PublicKey;
            }
            finally
            {
                _certCacheLock.Release();
            }

            return certResponse.PublicKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PayPal certificate for cert_id: {CertId}", certId);
            return null;
        }
    }

    private static byte[] ComputeSHA256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
    }

    private bool VerifySignature(string data, string signature, string publicKey)
    {
        try
        {
            // Remove PEM headers and format the public key
            var cleanedKey = publicKey
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(cleanedKey);
            var signatureBytes = Convert.FromBase64String(signature);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying RSA signature");
            return false;
        }
    }

    private class PayPalCertificateResponse
    {
        public string? PublicKey { get; set; }
    }
}

// Webhook validation middleware
public class PayPalWebhookValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PayPalWebhookValidationMiddleware> _logger;

    public PayPalWebhookValidationMiddleware(
        RequestDelegate next, 
        ILogger<PayPalWebhookValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate PayPal webhook endpoints
        if (context.Request.Path.StartsWithSegments("/api/payments/webhook/paypal") ||
            context.Request.Path.StartsWithSegments("/api/disputes/webhook/paypal"))
        {
            // Resolve validator from request scope
            var validator = context.RequestServices.GetRequiredService<IPayPalWebhookValidator>();
            
            if (!await ValidatePayPalWebhookAsync(context, validator))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid webhook signature");
                return;
            }
        }

        await _next(context);
    }

    private async Task<bool> ValidatePayPalWebhookAsync(HttpContext context, IPayPalWebhookValidator validator)
    {
        try
        {
            // Extract headers
            var headers = context.Request.Headers;
            
            if (!headers.TryGetValue("PAYPAL-AUTH-ALGO", out var authAlgo) ||
                !headers.TryGetValue("PAYPAL-TRANSMISSION-ID", out var transmissionId) ||
                !headers.TryGetValue("PAYPAL-CERT-ID", out var certId) ||
                !headers.TryGetValue("PAYPAL-TRANSMISSION-TIME", out var transmissionTime) ||
                !headers.TryGetValue("PAYPAL-TRANSMISSION-SIG", out var signature))
            {
                _logger.LogWarning("Missing required PayPal webhook headers");
                return false;
            }

            // Read webhook body
            var bodyStream = context.Request.Body;
            using var reader = new StreamReader(bodyStream, leaveOpen: true);
            var webhookBody = await reader.ReadToEndAsync();

            // Get webhook ID from configuration based on endpoint
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            string? webhookId;
            
            if (context.Request.Path.StartsWithSegments("/api/disputes/webhook/paypal"))
            {
                webhookId = configuration["Payment:PayPal:DisputeWebhookId"];
                if (string.IsNullOrEmpty(webhookId))
                {
                    _logger.LogError("PayPal dispute webhook ID not configured");
                    return false;
                }
            }
            else
            {
                webhookId = configuration["Payment:PayPal:WebhookId"];
                if (string.IsNullOrEmpty(webhookId))
                {
                    _logger.LogError("PayPal payment webhook ID not configured");
                    return false;
                }
            }

            // Validate signature
            return await validator.ValidateWebhookSignatureAsync(
                webhookBody, 
                authAlgo!, 
                transmissionId!, 
                certId!, 
                transmissionTime!, 
                signature!, 
                webhookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PayPal webhook validation");
            return false;
        }
    }
}