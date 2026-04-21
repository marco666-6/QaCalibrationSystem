namespace Project.Domain.Entities;

public sealed class User
{
    public long UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public long? EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Employee? Employee { get; set; }

    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTime.UtcNow;
}
