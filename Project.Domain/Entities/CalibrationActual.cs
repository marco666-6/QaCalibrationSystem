namespace Project.Domain.Entities;

public sealed class CalibrationActual
{
    public long Id { get; set; }
    public long HeaderId { get; set; }
    public string CalibStatus { get; set; } = "G";
    public DateTime? CompletedDt { get; set; }
    public string? CompletedBy { get; set; }
}
