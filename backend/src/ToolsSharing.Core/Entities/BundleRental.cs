using System;
using System.Collections.Generic;

namespace ToolsSharing.Core.Entities
{
    public class BundleRental : BaseEntity
    {
        public Guid BundleId { get; set; }
        public Bundle Bundle { get; set; } = null!;
        
        public string RenterUserId { get; set; } = "";
        public User RenterUser { get; set; } = null!;
        
        public DateTime RentalDate { get; set; }
        public DateTime ReturnDate { get; set; }
        
        // Pricing
        public decimal TotalCost { get; set; }
        public decimal BundleDiscountAmount { get; set; }
        public decimal FinalCost { get; set; }
        
        // Status
        public string Status { get; set; } = "Pending"; // Pending, Approved, Active, Completed, Cancelled
        
        // Individual tool rentals created from this bundle
        public ICollection<Rental> ToolRentals { get; set; } = new List<Rental>();
        
        // Notes
        public string? RenterNotes { get; set; }
        public string? OwnerNotes { get; set; }
    }
}