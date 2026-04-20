namespace Project.Domain.Entities;

public sealed class Employee
{
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public long SectionId { get; set; }
    public long? PositionId { get; set; }
    public long? ManagerId { get; set; }
    public string EmploymentStatus { get; set; } = "Active";
    public string? ProfilePhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
