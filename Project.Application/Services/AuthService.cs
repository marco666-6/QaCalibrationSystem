using FluentValidation;
using Microsoft.Extensions.Logging;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterEmployeeRequest> _registerValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ConfirmResetPasswordRequest> _confirmResetPasswordValidator;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;
    private const int ResetPasswordTokenMinutes = 30;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterEmployeeRequest> registerValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ConfirmResetPasswordRequest> confirmResetPasswordValidator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _changePasswordValidator = changePasswordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _confirmResetPasswordValidator = confirmResetPasswordValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoginResponse>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByUsernameAsync(request.Username.Trim());
        if (user is null || !user.IsActive)
            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");

        if (user.IsLockedOut)
            return ApiResponse<LoginResponse>.Fail("Account is temporarily locked.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _userRepository.IncrementFailedLoginAttemptsAsync(user.UserId);
            if (user.FailedLoginAttempts + 1 >= MaxFailedAttempts)
            {
                await _userRepository.LockAccountAsync(user.UserId, DateTime.UtcNow.AddMinutes(LockoutMinutes));
                return ApiResponse<LoginResponse>.Fail("Account is temporarily locked.");
            }

            return ApiResponse<LoginResponse>.Fail("Invalid username or password.");
        }

        await _userRepository.ResetFailedLoginAttemptsAsync(user.UserId);
        await _userRepository.UpdateLastLoginAsync(user.UserId);
        return await BuildLoginResponseAsync(user);
    }

    public async Task<ApiResponse<LoginResponse>> RegisterEmployeeAsync(RegisterEmployeeRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LoginResponse>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var employee = await _userRepository.GetEmployeeRegistrationCandidateAsync(request.EmployeeCode.Trim());
        if (employee is null)
            return ApiResponse<LoginResponse>.Fail("Employee was not found or already has a user account.");

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim()))
            return ApiResponse<LoginResponse>.Fail($"Username '{request.Username}' is already in use.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail))
            return ApiResponse<LoginResponse>.Fail($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            EmployeeId = employee.EmployeeId,
            Username = request.Username.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = RoleConstants.Default,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        user.UserId = await _userRepository.CreateAsync(user);
        _logger.LogInformation("Employee self-registration created user {UserId}", user.UserId);

        var created = await _userRepository.GetByIdAsync(user.UserId) ?? user;
        return await BuildLoginResponseAsync(created);
    }

    public async Task<ApiResponse> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse.NotFound("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return ApiResponse.Fail("Current password is incorrect.");

        await _userRepository.UpdatePasswordAsync(userId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword), mustChangePassword: false);
        await _userRepository.ResetFailedLoginAttemptsAsync(userId);
        return ApiResponse.Ok("Password changed successfully.");
    }

    public async Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);
        if (user is null || !user.IsActive)
            return ApiResponse<RefreshTokenResponse>.Fail("Invalid or expired refresh token.");

        var token = _jwtTokenService.GenerateToken(user);
        return ApiResponse<RefreshTokenResponse>.Ok(new RefreshTokenResponse(token, DateTime.UtcNow.AddMinutes(60)));
    }

    public async Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<ForgotPasswordResponse>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail.Trim());
        if (user is null || !user.IsActive)
            return ApiResponse<ForgotPasswordResponse>.Fail("User was not found for the provided username/email.");

        await _userRepository.InvalidatePasswordResetTokensAsync(user.UserId);

        var resetToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(ResetPasswordTokenMinutes);
        await _userRepository.CreatePasswordResetTokenAsync(user.UserId, resetToken, expiresAt);

        _logger.LogInformation("Password reset token created for user {UserId}", user.UserId);
        return ApiResponse<ForgotPasswordResponse>.Ok(new ForgotPasswordResponse(resetToken, expiresAt), "Password reset token created.");
    }

    public async Task<ApiResponse> ConfirmResetPasswordAsync(ConfirmResetPasswordRequest request)
    {
        var validation = await _confirmResetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var token = await _userRepository.GetPasswordResetTokenAsync(request.ResetToken.Trim());
        if (token is null || token.ConsumedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse.Fail("Invalid or expired password reset token.");

        var user = await _userRepository.GetByIdAsync(token.UserId);
        if (user is null || !user.IsActive)
            return ApiResponse.Fail("User account is invalid or inactive.");

        await _userRepository.UpdatePasswordAsync(user.UserId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword), mustChangePassword: false);
        await _userRepository.ResetFailedLoginAttemptsAsync(user.UserId);
        await _userRepository.ConsumePasswordResetTokenAsync(token.Id, DateTime.UtcNow);
        await _userRepository.StoreRefreshTokenAsync(user.UserId, string.Empty, DateTime.UtcNow);

        return ApiResponse.Ok("Password reset successfully.");
    }

    private async Task<ApiResponse<LoginResponse>> BuildLoginResponseAsync(User user)
    {
        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(7);
        await _userRepository.StoreRefreshTokenAsync(user.UserId, refreshToken, refreshExpiresAt);

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(
            token,
            DateTime.UtcNow.AddMinutes(60),
            refreshToken,
            refreshExpiresAt,
            user.UserId,
            user.EmployeeId,
            user.Username,
            user.Email,
            user.Role,
            user.MustChangePassword,
            MapEmployee(user.Employee)));
    }

    private static EmployeeIdentityDto? MapEmployee(Employee? employee)
        => employee is null
            ? null
            : new EmployeeIdentityDto(employee.EmployeeId, employee.EmployeeCode, employee.FullName, employee.Email);
}
