
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Rentals;

public record ApproveRentalCommand(Guid RentalId, string OwnerId);