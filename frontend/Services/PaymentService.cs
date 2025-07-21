using System.Net.Http.Json;
using frontend.Models;

namespace frontend.Services;

public interface IPaymentService
{
    Task<ApiResponse<PaymentInitiationResponse>> InitiatePaymentAsync(Guid rentalId);
    Task<ApiResponse<PaymentCompletionResponse>> CompletePaymentAsync(string paymentId, string payerId);
    Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(string paymentId);
    Task<ApiResponse<object>> CancelPaymentAsync(Guid rentalId); // Used for auto-cancellation
    Task<ApiResponse<TransactionDetailsResponse>> GetTransactionDetailsAsync(Guid rentalId);
    Task<ApiResponse<CalculateFeesResponse>> CalculateFeesAsync(decimal rentalAmount, decimal securityDeposit);
    Task<ApiResponse<PaymentSettingsResponse>> GetPaymentSettingsAsync();
    Task<ApiResponse<object>> UpdatePaymentSettingsAsync(UpdatePaymentSettingsRequest request);
    Task<ApiResponse<RefundResponse>> RequestRefundAsync(Guid rentalId, RefundRequest request);
    Task<ApiResponse<RefundResponse>> RefundSecurityDepositAsync(Guid rentalId);
    Task<ApiResponse<CanReceivePaymentsResponse>> CanOwnerReceivePaymentsAsync(string ownerId);
    Task<ApiResponse<RentalCostCalculationResponse>> CalculateRentalCostAsync(Guid toolId, DateTime startDate, DateTime endDate);
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<PaymentInitiationResponse>> InitiatePaymentAsync(Guid rentalId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/payments/initiate/{rentalId}", null);
            return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentInitiationResponse>>() 
                ?? new ApiResponse<PaymentInitiationResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PaymentInitiationResponse> 
            { 
                Success = false, 
                Message = $"Error initiating payment: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PaymentCompletionResponse>> CompletePaymentAsync(string paymentId, string payerId)
    {
        try
        {
            var request = new { paymentId, payerId };
            var response = await _httpClient.PostAsJsonAsync("api/payments/complete", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentCompletionResponse>>() 
                ?? new ApiResponse<PaymentCompletionResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PaymentCompletionResponse> 
            { 
                Success = false, 
                Message = $"Error completing payment: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatusAsync(string paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payments/status/{paymentId}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentStatusResponse>>() 
                ?? new ApiResponse<PaymentStatusResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PaymentStatusResponse> 
            { 
                Success = false, 
                Message = $"Error getting payment status: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<TransactionDetailsResponse>> GetTransactionDetailsAsync(Guid rentalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payments/transaction/{rentalId}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<TransactionDetailsResponse>>() 
                ?? new ApiResponse<TransactionDetailsResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<TransactionDetailsResponse> 
            { 
                Success = false, 
                Message = $"Error getting transaction details: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<CalculateFeesResponse>> CalculateFeesAsync(decimal rentalAmount, decimal securityDeposit)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payments/calculate-fees?rentalAmount={rentalAmount}&securityDeposit={securityDeposit}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<CalculateFeesResponse>>() 
                ?? new ApiResponse<CalculateFeesResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<CalculateFeesResponse> 
            { 
                Success = false, 
                Message = $"Error calculating fees: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<PaymentSettingsResponse>> GetPaymentSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/payments/settings");
            return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentSettingsResponse>>() 
                ?? new ApiResponse<PaymentSettingsResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PaymentSettingsResponse> 
            { 
                Success = false, 
                Message = $"Error getting payment settings: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<object>> UpdatePaymentSettingsAsync(UpdatePaymentSettingsRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/payments/settings", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>() 
                ?? new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> 
            { 
                Success = false, 
                Message = $"Error updating payment settings: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<RefundResponse>> RequestRefundAsync(Guid rentalId, RefundRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/payments/refund/{rentalId}", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<RefundResponse>>() 
                ?? new ApiResponse<RefundResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<RefundResponse> 
            { 
                Success = false, 
                Message = $"Error requesting refund: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<RefundResponse>> RefundSecurityDepositAsync(Guid rentalId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/payments/refund-deposit/{rentalId}", null);
            return await response.Content.ReadFromJsonAsync<ApiResponse<RefundResponse>>() 
                ?? new ApiResponse<RefundResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<RefundResponse> 
            { 
                Success = false, 
                Message = $"Error refunding security deposit: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<CanReceivePaymentsResponse>> CanOwnerReceivePaymentsAsync(string ownerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payments/can-receive-payments/{ownerId}");
            return await response.Content.ReadFromJsonAsync<ApiResponse<CanReceivePaymentsResponse>>() 
                ?? new ApiResponse<CanReceivePaymentsResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<CanReceivePaymentsResponse> 
            { 
                Success = false, 
                Message = $"Error checking payment settings: {ex.Message}" 
            };
        }
    }

    public async Task<ApiResponse<RentalCostCalculationResponse>> CalculateRentalCostAsync(Guid toolId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"api/payments/calculate-rental-cost?toolId={toolId}&startDate={startDateStr}&endDate={endDateStr}");
            
            return await response.Content.ReadFromJsonAsync<ApiResponse<RentalCostCalculationResponse>>() 
                ?? new ApiResponse<RentalCostCalculationResponse> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<RentalCostCalculationResponse> 
            { 
                Success = false, 
                Message = $"Error calculating rental cost: {ex.Message}" 
            };
        }
    }

    // Internal method used only for auto-cancellation in PaymentComplete page
    public async Task<ApiResponse<object>> CancelPaymentAsync(Guid rentalId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/payments/cancel/{rentalId}", null);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>() 
                ?? new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Failed to parse response" 
                };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> 
            { 
                Success = false, 
                Message = $"Error cancelling payment: {ex.Message}" 
            };
        }
    }
}

// Note: Models moved to PaymentModels.cs to avoid duplication