namespace Project.Domain.Entities;

public sealed class CalibrationEquipment
{
    public long Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string ControlNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string Location { get; set; } = string.Empty;
    public long SectionId { get; set; }
    public long PicId { get; set; }
    public string PicCode { get; set; } = string.Empty;
    public string PicFullName { get; set; } = string.Empty;
    public int CalibIntervalMonths { get; set; }
    public DateOnly LastCalibDate { get; set; }
    public int LastCalibMonth { get; set; }
    public DateOnly NextCalibDate { get; set; }
    public int NextCalibMonth { get; set; }
    public string CalibType { get; set; } = "I";
    public string EquipmentStatus { get; set; } = "A";
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}
