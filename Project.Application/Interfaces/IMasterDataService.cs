using Project.Application.Common;
using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface IMasterDataService
{
    Task<ApiResponse<PagedResult<SectionDto>>> GetSectionsAsync(SectionFilterParams filters);
    Task<ApiResponse<SectionDto>> CreateSectionAsync(SaveSectionRequest request);
    Task<ApiResponse<SectionDto>> UpdateSectionAsync(long sectionId, SaveSectionRequest request);

    Task<ApiResponse<PagedResult<PositionDto>>> GetPositionsAsync(PositionFilterParams filters);
    Task<ApiResponse<PositionDto>> CreatePositionAsync(SavePositionRequest request);
    Task<ApiResponse<PositionDto>> UpdatePositionAsync(long positionId, SavePositionRequest request);

    Task<ApiResponse<PagedResult<LocationDto>>> GetLocationsAsync(LocationFilterParams filters);
    Task<ApiResponse<LocationDto>> CreateLocationAsync(SaveLocationRequest request);
    Task<ApiResponse<LocationDto>> UpdateLocationAsync(long locationId, SaveLocationRequest request);

    Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesAsync(EmployeeFilterParams filters);
    Task<ApiResponse<EmployeeDto>> GetEmployeeByIdAsync(long employeeId);
    Task<ApiResponse<IEnumerable<EmployeeOptionDto>>> GetEmployeeOptionsAsync(string? search, int top);
    Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(SaveEmployeeRequest request);
    Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(long employeeId, SaveEmployeeRequest request);
}
