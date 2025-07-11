namespace frontend.Models;

public class ConsentResponse
{
    public int ConsentType { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentDate { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public DateTime? WithdrawnDate { get; set; }
}

public class DataRequestResponse
{
    public Guid Id { get; set; }
    public int RequestType { get; set; }
    public object Status { get; set; } = null!;
    public DateTime RequestDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? ResponseData { get; set; }
}