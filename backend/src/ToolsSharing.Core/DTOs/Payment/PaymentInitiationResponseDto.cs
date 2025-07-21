namespace ToolsSharing.Core.DTOs.Payment;

public class PaymentInitiationResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
}