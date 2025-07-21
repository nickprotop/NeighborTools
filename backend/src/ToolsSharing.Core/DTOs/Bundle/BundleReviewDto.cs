using System;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class BundleReviewDto
    {
        public Guid Id { get; set; }
        public Guid BundleId { get; set; }
        public Guid? BundleRentalId { get; set; }
        public string ReviewerId { get; set; } = "";
        public string ReviewerName { get; set; } = "";
        public string ReviewerAvatar { get; set; } = "";
        public int Rating { get; set; }
        public string Title { get; set; } = "";
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        
        // Bundle-specific context
        public string BundleName { get; set; } = "";
        public DateTime? RentalStartDate { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public int RentalDuration { get; set; } // days
    }
    
    public class CreateBundleReviewRequest
    {
        public Guid BundleId { get; set; }
        public Guid? BundleRentalId { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = "";
        public string Comment { get; set; } = "";
    }
    
    public class BundleReviewSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
        public List<BundleReviewDto> LatestReviews { get; set; } = new();
    }
}