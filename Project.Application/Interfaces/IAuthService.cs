using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RegisterEmployeeAsync(RegisterEmployeeRequest request);
    Task<ApiResponse> ChangePasswordAsync(long userId, ChangePasswordRequest request);
    Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ConfirmResetPasswordAsync(ConfirmResetPasswordRequest request);
    Task<ApiResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);
}
