namespace Project.Domain.Entities;

public sealed class Location
{
    public long LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
