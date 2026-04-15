using FluentValidation;
using Project.Application.Common;

namespace Project.Application.DTOs;

public sealed record LoginRequest(string Username, string Password);

public sealed record RegisterEmployeeRequest(
    string EmployeeCode,
    string Username,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record EmployeeIdentityDto(
    long EmployeeId,
    string EmployeeCode,
    string FullName,
    string? Email);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    long UserId,
    long? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword,
    EmployeeIdentityDto? Employee);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RefreshTokenResponse(string Token, DateTime ExpiresAt);
public sealed record ForgotPasswordRequest(string UsernameOrEmail);
public sealed record ForgotPasswordResponse(string ResetToken, DateTime ExpiresAt);
public sealed record ConfirmResetPasswordRequest(string ResetToken, string NewPassword, string ConfirmNewPassword);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RegisterEmployeeRequestValidator : AbstractValidator<RegisterEmployeeRequest>
{
    public RegisterEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().Length(6);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().MaximumLength(200);
    }
}

public sealed class ConfirmResetPasswordRequestValidator : AbstractValidator<ConfirmResetPasswordRequest>
{
    public ConfirmResetPasswordRequestValidator()
    {
        RuleFor(x => x.ResetToken).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
    }
}

public static class RoleConstants
{
    public const string Admin = "admin";
    public const string Default = "user";
}

public sealed record UserDto(
    long UserId,
    long? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    int FailedLoginAttempts,
    bool MustChangePassword,
    DateTime? LastLogin,
    DateTime? LockoutUntil,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    EmployeeIdentityDto? Employee);

public sealed record UserSummaryDto(
    long UserId,
    long? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLogin,
    string? EmployeeCode,
    string? FullName);

public sealed record UserOptionDto(
    long UserId,
    string Username,
    string Email,
    string Role,
    string? EmployeeCode,
    string? FullName);

public sealed class UserFilterParams : PaginationParams
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class UserOptionFilterParams
{
    private const int MaxTop = 50;
    private int _top = 20;

    public string? Search { get; set; }

    public int Top
    {
        get => _top;
        set => _top = value < 1 ? 1 : value > MaxTop ? MaxTop : value;
    }
}

public sealed record CreateUserRequest(
    long? EmployeeId,
    string Username,
    string Email,
    string Password,
    string Role,
    bool IsActive,
    bool MustChangePassword);

public sealed record UpdateUserRequest(
    long? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    bool MustChangePassword);

public sealed record MyProfileDto(
    long UserId,
    long? EmployeeId,
    string Username,
    string Email,
    string Role,
    bool MustChangePassword,
    EmployeeIdentityDto? Employee);

public sealed record UpdateMyProfileRequest(string Username, string Email);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50);
    }
}

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}

public sealed record ResetPasswordRequest(string NewPassword, bool MustChangePassword = true);

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"[0-9]");
    }
}
