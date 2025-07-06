
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Rentals;

public record RejectRentalCommand(Guid RentalId, string OwnerId, string Reason);