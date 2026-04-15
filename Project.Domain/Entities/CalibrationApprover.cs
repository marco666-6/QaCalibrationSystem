namespace Project.Domain.Entities;

public sealed class CalibrationApprover
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string StepNo { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}
