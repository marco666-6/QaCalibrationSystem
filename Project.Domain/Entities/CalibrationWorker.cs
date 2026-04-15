namespace Project.Domain.Entities;

public sealed class CalibrationWorker
{
    public long Id { get; set; }
    public long ActualId { get; set; }
    public long? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeFullName { get; set; }
    public string? ExternalPartyName { get; set; }
    public bool IsPic { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
