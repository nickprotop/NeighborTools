using System;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class CreateBundleRentalRequest
    {
        public Guid BundleId { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? RenterNotes { get; set; }
        public List<Guid> SelectedToolIds { get; set; } = new(); // Optional tools selection
    }
    
    public class BundleRentalDto
    {
        public Guid Id { get; set; }
        public Guid BundleId { get; set; }
        public string BundleName { get; set; } = "";
        public string RenterUserId { get; set; } = "";
        public string RenterName { get; set; } = "";
        public DateTime RentalDate { get; set; }
        public DateTime ReturnDate { get; set; }
        
        // Pricing
        public decimal TotalCost { get; set; }
        public decimal BundleDiscountAmount { get; set; }
        public decimal FinalCost { get; set; }
        
        // Status
        public string Status { get; set; } = "";
        
        // Individual tool rentals
        public List<BundleToolRentalDto> ToolRentals { get; set; } = new();
        
        // Notes
        public string? RenterNotes { get; set; }
        public string? OwnerNotes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class BundleToolRentalDto
    {
        public Guid RentalId { get; set; }
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Cost { get; set; }
    }
}