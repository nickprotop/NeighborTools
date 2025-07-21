using System;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class BundleAvailabilityRequest
    {
        public Guid BundleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    
    public class BundleAvailabilityResponse
    {
        public bool IsAvailable { get; set; }
        public DateTime? EarliestAvailableDate { get; set; }
        public string Message { get; set; } = "";
        public BundleCostCalculationResponse? CostCalculation { get; set; }
        public List<ToolAvailabilityStatus> ToolAvailability { get; set; } = new();
    }
    
    public class ToolAvailabilityStatus
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public bool IsAvailable { get; set; }
        public DateTime? AvailableFromDate { get; set; }
        public string UnavailabilityReason { get; set; } = "";
        public bool IsOptional { get; set; }
    }
    
    public class BundleCostCalculationResponse
    {
        public decimal TotalCost { get; set; }
        public decimal BundleDiscountAmount { get; set; }
        public decimal FinalCost { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal GrandTotal { get; set; }
        public List<ToolCostBreakdown> ToolCosts { get; set; } = new();
    }
    
    public class ToolCostBreakdown
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public decimal DailyRate { get; set; }
        public int RentalDays { get; set; }
        public int QuantityNeeded { get; set; }
        public decimal TotalCost { get; set; }
    }
}