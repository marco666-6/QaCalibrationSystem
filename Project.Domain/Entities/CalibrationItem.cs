namespace Project.Domain.Entities;

public sealed class CalibrationItem
{
    public long Id { get; set; }
    public long HeaderId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int ItemCompleted { get; set; }
    public string? StdUsed { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}
