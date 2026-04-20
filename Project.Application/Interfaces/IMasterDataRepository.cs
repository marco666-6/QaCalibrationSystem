using Project.Application.DTOs;
using Project.Domain.Entities;

namespace Project.Application.Interfaces;

public interface IMasterDataRepository
{
    Task<(IEnumerable<Section> Items, int TotalCount)> GetSectionsAsync(SectionFilterParams filters);
    Task<Section?> GetSectionByIdAsync(long sectionId);
    Task<bool> SectionCodeExistsAsync(string sectionCode, long? excludeId = null);
    Task<long> CreateSectionAsync(Section section);
    Task<bool> UpdateSectionAsync(Section section);

    Task<(IEnumerable<Position> Items, int TotalCount)> GetPositionsAsync(PositionFilterParams filters);
    Task<Position?> GetPositionByIdAsync(long positionId);
    Task<bool> PositionCodeExistsAsync(string positionCode, long? excludeId = null);
    Task<long> CreatePositionAsync(Position position);
    Task<bool> UpdatePositionAsync(Position position);

    Task<(IEnumerable<Location> Items, int TotalCount)> GetLocationsAsync(LocationFilterParams filters);
    Task<Location?> GetLocationByIdAsync(long locationId);
    Task<bool> LocationNameExistsAsync(string locationName, long? excludeId = null);
    Task<long> CreateLocationAsync(Location location);
    Task<bool> UpdateLocationAsync(Location location);

    Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetEmployeesAsync(EmployeeFilterParams filters);
    Task<EmployeeDto?> GetEmployeeByIdAsync(long employeeId);
    Task<IEnumerable<EmployeeOptionDto>> GetEmployeeOptionsAsync(string? search, int top);
    Task<bool> EmployeeCodeExistsAsync(string employeeCode, long? excludeId = null);
    Task<bool> EmployeeEmailExistsAsync(string? email, long? excludeId = null);
    Task<long> CreateEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> EmployeeExistsAsync(long employeeId);
    Task<bool> SectionExistsAsync(long sectionId);
    Task<bool> PositionExistsAsync(long positionId);
}
