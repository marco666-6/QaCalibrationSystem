namespace Project.Domain.Entities;

public sealed class CalibrationEquipmentDetail
{
    public long Id { get; set; }
    public long DetailId { get; set; }
    public long EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string ControlNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string Location { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public int CalibIntervalMonths { get; set; }
    public DateOnly LastCalibDate { get; set; }
    public int LastCalibMonth { get; set; }
    public DateOnly NextCalibDate { get; set; }
    public int NextCalibMonth { get; set; }
    public string PicCode { get; set; } = string.Empty;
    public string PicFullName { get; set; } = string.Empty;
}
