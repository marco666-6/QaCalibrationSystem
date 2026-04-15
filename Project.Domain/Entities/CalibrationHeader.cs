namespace Project.Domain.Entities;

public sealed class CalibrationHeader
{
    public long Id { get; set; }
    public string CalibNo { get; set; } = string.Empty;
    public string CalibPhase { get; set; } = "P";
    public string CalibType { get; set; } = string.Empty;
    public int CalibMonth { get; set; }
    public int CalibYear { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}
