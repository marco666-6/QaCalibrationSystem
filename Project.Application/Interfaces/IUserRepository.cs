using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<Employee?> GetEmployeeRegistrationCandidateAsync(string employeeCode);
    Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(UserFilterParams filters);
    Task<IEnumerable<User>> GetOptionsAsync(UserOptionFilterParams filters);
    Task<bool> UsernameExistsAsync(string username, long? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, long? excludeUserId = null);
    Task<bool> EmployeeAlreadyAssignedAsync(long employeeId, long? excludeUserId = null);
    Task<long> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> UpdatePasswordAsync(long userId, string newPasswordHash, bool mustChangePassword);
    Task<bool> SoftDeleteAsync(long userId);
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
    Task<long> CreatePasswordResetTokenAsync(long userId, string token, DateTime expiresAt);
    Task InvalidatePasswordResetTokensAsync(long userId);
    Task ConsumePasswordResetTokenAsync(long passwordResetTokenId, DateTime consumedAt);
    Task UpdateLastLoginAsync(long userId);
    Task IncrementFailedLoginAttemptsAsync(long userId);
    Task ResetFailedLoginAttemptsAsync(long userId);
    Task LockAccountAsync(long userId, DateTime lockoutUntil);
    Task StoreRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt);
}
