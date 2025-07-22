using System.ComponentModel.DataAnnotations;

namespace ToolsSharing.Core.DTOs.Bundle
{
    public class RequestApprovalRequest
    {
        [MaxLength(500)]
        public string? Message { get; set; }
    }
}