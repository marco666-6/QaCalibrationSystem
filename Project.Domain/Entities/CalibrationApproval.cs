namespace Project.Domain.Entities;

public sealed class CalibrationApproval
{
    public long Id { get; set; }
    public long HeaderId { get; set; }
    public string StepNo { get; set; } = string.Empty;
    public long? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeFullName { get; set; }
    public string Action { get; set; } = "C";
    public string? Remarks { get; set; }
    public DateTime? ActionedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
