using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserSummaryDto>>> GetAllAsync(UserFilterParams filters);
    Task<ApiResponse<IEnumerable<UserOptionDto>>> GetOptionsAsync(UserOptionFilterParams filters);
    Task<ApiResponse<UserDto>> GetByIdAsync(long userId);
    Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateAsync(long userId, UpdateUserRequest request);
    Task<ApiResponse<MyProfileDto>> GetMyProfileAsync(long userId);
    Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(long userId, UpdateMyProfileRequest request);
    Task<ApiResponse> ResetPasswordAsync(long userId, ResetPasswordRequest request);
    Task<ApiResponse> DeleteAsync(long userId);
}
