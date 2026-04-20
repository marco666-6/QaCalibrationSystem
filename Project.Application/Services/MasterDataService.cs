using FluentValidation;
using Project.Application.Common;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;

namespace Project.Application.Services;

public sealed class MasterDataService : IMasterDataService
{
    private readonly IMasterDataRepository _repository;
    private readonly IValidator<SaveSectionRequest> _sectionValidator;
    private readonly IValidator<SavePositionRequest> _positionValidator;
    private readonly IValidator<SaveLocationRequest> _locationValidator;
    private readonly IValidator<SaveEmployeeRequest> _employeeValidator;

    public MasterDataService(
        IMasterDataRepository repository,
        IValidator<SaveSectionRequest> sectionValidator,
        IValidator<SavePositionRequest> positionValidator,
        IValidator<SaveLocationRequest> locationValidator,
        IValidator<SaveEmployeeRequest> employeeValidator)
    {
        _repository = repository;
        _sectionValidator = sectionValidator;
        _positionValidator = positionValidator;
        _locationValidator = locationValidator;
        _employeeValidator = employeeValidator;
    }

    public async Task<ApiResponse<PagedResult<SectionDto>>> GetSectionsAsync(SectionFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetSectionsAsync(filters);
        return ApiResponse<PagedResult<SectionDto>>.Ok(PagedResult<SectionDto>.Create(items.Select(MapSection), totalCount, filters));
    }

    public async Task<ApiResponse<SectionDto>> CreateSectionAsync(SaveSectionRequest request)
    {
        var validation = await _sectionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SectionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.SectionCodeExistsAsync(request.SectionCode.Trim()))
            return ApiResponse<SectionDto>.Fail($"Section code '{request.SectionCode}' is already in use.");

        var entity = new Section
        {
            SectionCode = request.SectionCode.Trim(),
            SectionName = request.SectionName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        entity.SectionId = await _repository.CreateSectionAsync(entity);
        return ApiResponse<SectionDto>.Created(MapSection(entity));
    }

    public async Task<ApiResponse<SectionDto>> UpdateSectionAsync(long sectionId, SaveSectionRequest request)
    {
        var validation = await _sectionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<SectionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var entity = await _repository.GetSectionByIdAsync(sectionId);
        if (entity is null)
            return ApiResponse<SectionDto>.NotFound("Section not found.");

        if (await _repository.SectionCodeExistsAsync(request.SectionCode.Trim(), sectionId))
            return ApiResponse<SectionDto>.Fail($"Section code '{request.SectionCode}' is already in use.");

        entity.SectionCode = request.SectionCode.Trim();
        entity.SectionName = request.SectionName.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateSectionAsync(entity);
        return ApiResponse<SectionDto>.Ok(MapSection(entity), "Updated successfully.");
    }

    public async Task<ApiResponse<PagedResult<PositionDto>>> GetPositionsAsync(PositionFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetPositionsAsync(filters);
        return ApiResponse<PagedResult<PositionDto>>.Ok(PagedResult<PositionDto>.Create(items.Select(MapPosition), totalCount, filters));
    }

    public async Task<ApiResponse<PositionDto>> CreatePositionAsync(SavePositionRequest request)
    {
        var validation = await _positionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<PositionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.PositionCodeExistsAsync(request.PositionCode.Trim()))
            return ApiResponse<PositionDto>.Fail($"Position code '{request.PositionCode}' is already in use.");

        var entity = new Position
        {
            PositionCode = request.PositionCode.Trim(),
            PositionName = request.PositionName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        entity.PositionId = await _repository.CreatePositionAsync(entity);
        return ApiResponse<PositionDto>.Created(MapPosition(entity));
    }

    public async Task<ApiResponse<PositionDto>> UpdatePositionAsync(long positionId, SavePositionRequest request)
    {
        var validation = await _positionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<PositionDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var entity = await _repository.GetPositionByIdAsync(positionId);
        if (entity is null)
            return ApiResponse<PositionDto>.NotFound("Position not found.");

        if (await _repository.PositionCodeExistsAsync(request.PositionCode.Trim(), positionId))
            return ApiResponse<PositionDto>.Fail($"Position code '{request.PositionCode}' is already in use.");

        entity.PositionCode = request.PositionCode.Trim();
        entity.PositionName = request.PositionName.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdatePositionAsync(entity);
        return ApiResponse<PositionDto>.Ok(MapPosition(entity), "Updated successfully.");
    }

    public async Task<ApiResponse<PagedResult<LocationDto>>> GetLocationsAsync(LocationFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetLocationsAsync(filters);
        return ApiResponse<PagedResult<LocationDto>>.Ok(PagedResult<LocationDto>.Create(items.Select(MapLocation), totalCount, filters));
    }

    public async Task<ApiResponse<LocationDto>> CreateLocationAsync(SaveLocationRequest request)
    {
        var validation = await _locationValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LocationDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.LocationNameExistsAsync(request.LocationName.Trim()))
            return ApiResponse<LocationDto>.Fail($"Location '{request.LocationName}' already exists.");

        var entity = new Location
        {
            LocationName = request.LocationName.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        entity.LocationId = await _repository.CreateLocationAsync(entity);
        return ApiResponse<LocationDto>.Created(MapLocation(entity));
    }

    public async Task<ApiResponse<LocationDto>> UpdateLocationAsync(long locationId, SaveLocationRequest request)
    {
        var validation = await _locationValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<LocationDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var entity = await _repository.GetLocationByIdAsync(locationId);
        if (entity is null)
            return ApiResponse<LocationDto>.NotFound("Location not found.");

        if (await _repository.LocationNameExistsAsync(request.LocationName.Trim(), locationId))
            return ApiResponse<LocationDto>.Fail($"Location '{request.LocationName}' already exists.");

        entity.LocationName = request.LocationName.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateLocationAsync(entity);
        return ApiResponse<LocationDto>.Ok(MapLocation(entity), "Updated successfully.");
    }

    public async Task<ApiResponse<PagedResult<EmployeeDto>>> GetEmployeesAsync(EmployeeFilterParams filters)
    {
        var (items, totalCount) = await _repository.GetEmployeesAsync(filters);
        return ApiResponse<PagedResult<EmployeeDto>>.Ok(PagedResult<EmployeeDto>.Create(items, totalCount, filters));
    }

    public async Task<ApiResponse<EmployeeDto>> GetEmployeeByIdAsync(long employeeId)
    {
        var employee = await _repository.GetEmployeeByIdAsync(employeeId);
        return employee is null
            ? ApiResponse<EmployeeDto>.NotFound("Employee not found.")
            : ApiResponse<EmployeeDto>.Ok(employee);
    }

    public async Task<ApiResponse<IEnumerable<EmployeeOptionDto>>> GetEmployeeOptionsAsync(string? search, int top)
        => ApiResponse<IEnumerable<EmployeeOptionDto>>.Ok(await _repository.GetEmployeeOptionsAsync(search, top));

    public async Task<ApiResponse<EmployeeDto>> CreateEmployeeAsync(SaveEmployeeRequest request)
    {
        var validation = await _employeeValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<EmployeeDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        if (await _repository.EmployeeCodeExistsAsync(request.EmployeeCode.Trim()))
            return ApiResponse<EmployeeDto>.Fail($"Employee code '{request.EmployeeCode}' is already in use.");

        if (await _repository.EmployeeEmailExistsAsync(request.Email?.Trim().ToLowerInvariant()))
            return ApiResponse<EmployeeDto>.Fail($"Email '{request.Email}' is already in use.");

        if (!await _repository.SectionExistsAsync(request.SectionId))
            return ApiResponse<EmployeeDto>.Fail("Selected section was not found.");

        if (request.PositionId.HasValue && !await _repository.PositionExistsAsync(request.PositionId.Value))
            return ApiResponse<EmployeeDto>.Fail("Selected position was not found.");

        if (request.ManagerId.HasValue && !await _repository.EmployeeExistsAsync(request.ManagerId.Value))
            return ApiResponse<EmployeeDto>.Fail("Selected manager was not found.");

        var entity = new Employee
        {
            EmployeeCode = request.EmployeeCode.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            SectionId = request.SectionId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            EmploymentStatus = request.EmploymentStatus.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        entity.EmployeeId = await _repository.CreateEmployeeAsync(entity);
        var created = await _repository.GetEmployeeByIdAsync(entity.EmployeeId);
        return ApiResponse<EmployeeDto>.Created(created!);
    }

    public async Task<ApiResponse<EmployeeDto>> UpdateEmployeeAsync(long employeeId, SaveEmployeeRequest request)
    {
        var validation = await _employeeValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ApiResponse<EmployeeDto>.Fail("Validation failed.", validation.Errors.Select(x => x.ErrorMessage));

        var existing = await _repository.GetEmployeeByIdAsync(employeeId);
        if (existing is null)
            return ApiResponse<EmployeeDto>.NotFound("Employee not found.");

        if (await _repository.EmployeeCodeExistsAsync(request.EmployeeCode.Trim(), employeeId))
            return ApiResponse<EmployeeDto>.Fail($"Employee code '{request.EmployeeCode}' is already in use.");

        if (await _repository.EmployeeEmailExistsAsync(request.Email?.Trim().ToLowerInvariant(), employeeId))
            return ApiResponse<EmployeeDto>.Fail($"Email '{request.Email}' is already in use.");

        if (!await _repository.SectionExistsAsync(request.SectionId))
            return ApiResponse<EmployeeDto>.Fail("Selected section was not found.");

        if (request.PositionId.HasValue && !await _repository.PositionExistsAsync(request.PositionId.Value))
            return ApiResponse<EmployeeDto>.Fail("Selected position was not found.");

        if (request.ManagerId.HasValue && !await _repository.EmployeeExistsAsync(request.ManagerId.Value))
            return ApiResponse<EmployeeDto>.Fail("Selected manager was not found.");

        var entity = new Employee
        {
            EmployeeId = employeeId,
            EmployeeCode = request.EmployeeCode.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            SectionId = request.SectionId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            EmploymentStatus = request.EmploymentStatus.Trim(),
            IsActive = request.IsActive,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpdateEmployeeAsync(entity);
        var updated = await _repository.GetEmployeeByIdAsync(employeeId);
        return ApiResponse<EmployeeDto>.Ok(updated!, "Updated successfully.");
    }

    private static SectionDto MapSection(Section x) => new(x.SectionId, x.SectionCode, x.SectionName, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static PositionDto MapPosition(Position x) => new(x.PositionId, x.PositionCode, x.PositionName, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static LocationDto MapLocation(Location x) => new(x.LocationId, x.LocationName, x.IsActive, x.CreatedAt, x.UpdatedAt);
}
