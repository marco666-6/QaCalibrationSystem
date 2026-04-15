namespace Project.Domain.Entities;

public sealed class CalibrationPlan
{
    public long Id { get; set; }
    public long HeaderId { get; set; }
    public string CalibStatus { get; set; } = "D";
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
}
