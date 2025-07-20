using ToolsSharing.Core.DTOs.Admin;

namespace ToolsSharing.Core.Interfaces;

public interface ISampleDataService
{
    Task<SampleDataStatusDto> GetStatusAsync();
    Task<SampleDataStatusDto> ApplySampleDataAsync(ApplySampleDataRequest request, string adminUserId);
    Task<SampleDataStatusDto> RemoveSampleDataAsync(RemoveSampleDataRequest request, string adminUserId);
    Task<bool> IsSampleDataAppliedAsync(string dataType);
    Task RemoveAllSampleDataAsync(string adminUserId);
}

public class SampleDataStatusDto
{
    public List<SampleDataTypeStatus> DataTypes { get; set; } = new();
    public bool HasAnySampleData { get; set; }
    public DateTime? LastAppliedAt { get; set; }
    public string? LastAppliedByUserId { get; set; }
    public string? LastAppliedByUserName { get; set; }
}

public class SampleDataTypeStatus
{
    public string DataType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsApplied { get; set; }
    public int Count { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? AppliedByUserId { get; set; }
    public string? AppliedByUserName { get; set; }
}

public class ApplySampleDataRequest
{
    public string[] DataTypes { get; set; } = Array.Empty<string>();
}

public class RemoveSampleDataRequest
{
    public string[] DataTypes { get; set; } = Array.Empty<string>();
}