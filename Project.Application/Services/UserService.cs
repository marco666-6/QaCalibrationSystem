using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UpdateMyProfileRequest> _profileValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public UserService(
        IUserRepository userRepository,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UpdateMyProfileRequest> profileValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _userRepository = userRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _profileValidator = profileValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    public async Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(UserFilterParams filters)
    {
        var (items, totalCount) = await _userRepository.GetAllAsync(filters);
        return ApiResponse<PagedResult<UserSummaryDto>>.Ok(
            PagedResult<UserSummaryDto>.Create(items.Select(MapSummary), totalCount, filters));
    }

    public async Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(UserOptionFilterParams filters)
    {
        var items = await _userRepository.GetOptionsAsync(filters);
        return ApiResponse<IEnumerable<UserOptionDto>>.Ok(items.Select(MapOption));
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null
            ? ApiResponse<UserDto>.NotFound("User not found.")
            : ApiResponse<UserDto>.Ok(MapDto(user));
    }

    public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim()))
            return ApiResponse<UserDto>.Fail($"Username '{request.Username}' is already in use.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail))
            return ApiResponse<UserDto>.Fail($"Email '{request.Email}' is already registered.");

        if (request.EmployeeId.HasValue && await _userRepository.EmployeeAlreadyAssignedAsync(request.EmployeeId.Value))
            return ApiResponse<UserDto>.Fail("The selected employee is already linked to another user.");

        var user = new User
        {
            EmployeeId = request.EmployeeId,
            EmployeeCode = null,
            Username = request.Username.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role.Trim(),
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            CreatedAt = DateTime.UtcNow
        };

        user.UserId = await _userRepository.CreateAsync(user);
        var created = await _userRepository.GetByIdAsync(user.UserId) ?? user;
        return ApiResponse<UserDto>.Created(MapDto(created));
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(long userId, UpdateUserRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<UserDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<UserDto>.NotFound("User not found.");

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim(), userId))
            return ApiResponse<UserDto>.Fail($"Username '{request.Username}' is already in use.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail, userId))
            return ApiResponse<UserDto>.Fail($"Email '{request.Email}' is already registered.");

        if (request.EmployeeId.HasValue && await _userRepository.EmployeeAlreadyAssignedAsync(request.EmployeeId.Value, userId))
            return ApiResponse<UserDto>.Fail("The selected employee is already linked to another user.");

        user.EmployeeId = request.EmployeeId;
        user.EmployeeCode = user.Employee?.EmployeeCode;
        user.Username = request.Username.Trim();
        user.Email = normalizedEmail;
        user.Role = request.Role.Trim();
        user.IsActive = request.IsActive;
        user.MustChangePassword = request.MustChangePassword;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        var updated = await _userRepository.GetByIdAsync(userId) ?? user;
        return ApiResponse<UserDto>.Ok(MapDto(updated), "Updated successfully.");
    }

    public async Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user is null
            ? ApiResponse<MyProfileDto>.NotFound("User not found.")
            : ApiResponse<MyProfileDto>.Ok(MapProfile(user));
    }

    public async Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(long userId, UpdateMyProfileRequest request)
    {
        var validation = await _profileValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<MyProfileDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<MyProfileDto>.NotFound("User not found.");

        if (await _userRepository.UsernameExistsAsync(request.Username.Trim(), userId))
            return ApiResponse<MyProfileDto>.Fail($"Username '{request.Username}' is already in use.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail, userId))
            return ApiResponse<MyProfileDto>.Fail($"Email '{request.Email}' is already registered.");

        user.Username = request.Username.Trim();
        user.Email = normalizedEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        var updated = await _userRepository.GetByIdAsync(userId) ?? user;
        return ApiResponse<MyProfileDto>.Ok(MapProfile(updated), "Profile updated successfully.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(long userId, ResetPasswordRequest request)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse.NotFound("User not found.");

        await _userRepository.UpdatePasswordAsync(userId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword), request.MustChangePassword);
        await _userRepository.ResetFailedLoginAttemptsAsync(userId);
        return ApiResponse.Ok("Password reset successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(long userId)
    {
        var deleted = await _userRepository.SoftDeleteAsync(userId);
        return deleted ? ApiResponse.Ok("User deleted successfully.") : ApiResponse.NotFound("User not found.");
    }

    private static UserDto MapDto(User user) => new(
        user.UserId,
        user.EmployeeId,
        user.Username,
        user.Email,
        user.Role,
        user.IsActive,
        user.FailedLoginAttempts,
        user.MustChangePassword,
        user.LastLogin,
        user.LockoutUntil,
        user.CreatedAt,
        user.UpdatedAt,
        MapEmployee(user.Employee));

    private static UserSummaryDto MapSummary(User user) => new(
        user.UserId,
        user.EmployeeId,
        user.Username,
        user.Email,
        user.Role,
        user.IsActive,
        user.MustChangePassword,
        user.LastLogin,
        user.Employee?.EmployeeCode,
        user.Employee?.FullName);

    private static UserOptionDto MapOption(User user) => new(
        user.UserId,
        user.Username,
        user.Email,
        user.Role,
        user.Employee?.EmployeeCode,
        user.Employee?.FullName);

    private static MyProfileDto MapProfile(User user) => new(
        user.UserId,
        user.EmployeeId,
        user.Username,
        user.Email,
        user.Role,
        user.MustChangePassword,
        MapEmployee(user.Employee));

    private static EmployeeIdentityDto? MapEmployee(Employee? employee)
        => employee is null
            ? null
            : new EmployeeIdentityDto(employee.EmployeeId, employee.EmployeeCode, employee.FullName, employee.Email);
}
