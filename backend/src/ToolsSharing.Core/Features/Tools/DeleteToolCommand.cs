
using ToolsSharing.Core.Common.Models;

namespace ToolsSharing.Core.Features.Tools;

public record DeleteToolCommand(Guid Id, string OwnerId);