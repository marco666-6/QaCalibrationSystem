namespace Project.Domain.Entities;

public sealed class CalibrationItemDetail
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public long EquipmentId { get; set; }
    public string? CalibResult { get; set; }
    public bool OverdueFlag { get; set; }
    public string? CertificateNo { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}
