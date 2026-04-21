using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class MasterDataRepository : BaseRepository<Section>, IMasterDataRepository
{
    public MasterDataRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(IEnumerable<Section> Items, int TotalCount)> GetSectionsAsync(SectionFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(section_code LIKE @Search OR section_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        using var connection = CreateConnection();
        var whereClause = string.Join(" AND ", where);
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.sections WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Section>($"""
            SELECT section_id, section_code, section_name, is_active, created_at, updated_at
            FROM dbo.sections
            WHERE {whereClause}
            ORDER BY section_code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public Task<Section?> GetSectionByIdAsync(long sectionId)
        => QuerySingleOrDefaultAsync("SELECT section_id, section_code, section_name, is_active, created_at, updated_at FROM dbo.sections WHERE section_id = @SectionId", new { SectionId = sectionId });

    public async Task<bool> SectionCodeExistsAsync(string sectionCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.sections WHERE section_code = @SectionCode AND (@ExcludeId IS NULL OR section_id <> @ExcludeId)", new { SectionCode = sectionCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateSectionAsync(Section section)
        => ExecuteScalarAsync<long>("INSERT INTO dbo.sections (section_code, section_name, is_active, created_at, updated_at) VALUES (@SectionCode, @SectionName, @IsActive, @CreatedAt, @UpdatedAt); SELECT CAST(SCOPE_IDENTITY() AS bigint);", section)!;

    public async Task<bool> UpdateSectionAsync(Section section)
        => await ExecuteAsync("UPDATE dbo.sections SET section_code = @SectionCode, section_name = @SectionName, is_active = @IsActive, updated_at = @UpdatedAt WHERE section_id = @SectionId", section) > 0;

    public async Task<(IEnumerable<Position> Items, int TotalCount)> GetPositionsAsync(PositionFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(position_code LIKE @Search OR position_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        using var connection = CreateConnection();
        var whereClause = string.Join(" AND ", where);
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.positions WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Position>($"""
            SELECT position_id, position_code, position_name, is_active, created_at, updated_at
            FROM dbo.positions
            WHERE {whereClause}
            ORDER BY position_code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<Position?> GetPositionByIdAsync(long positionId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Position>("SELECT position_id, position_code, position_name, is_active, created_at, updated_at FROM dbo.positions WHERE position_id = @PositionId", new { PositionId = positionId });
    }

    public async Task<bool> PositionCodeExistsAsync(string positionCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.positions WHERE position_code = @PositionCode AND (@ExcludeId IS NULL OR position_id <> @ExcludeId)", new { PositionCode = positionCode, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreatePositionAsync(Position position)
        => ExecuteScalarAsync<long>("INSERT INTO dbo.positions (position_code, position_name, is_active, created_at, updated_at) VALUES (@PositionCode, @PositionName, @IsActive, @CreatedAt, @UpdatedAt); SELECT CAST(SCOPE_IDENTITY() AS bigint);", position)!;

    public async Task<bool> UpdatePositionAsync(Position position)
        => await ExecuteAsync("UPDATE dbo.positions SET position_code = @PositionCode, position_name = @PositionName, is_active = @IsActive, updated_at = @UpdatedAt WHERE position_id = @PositionId", position) > 0;

    public async Task<(IEnumerable<Location> Items, int TotalCount)> GetLocationsAsync(LocationFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("location_name LIKE @Search");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        using var connection = CreateConnection();
        var whereClause = string.Join(" AND ", where);
        var total = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.locations WHERE {whereClause}", parameters);
        var items = await connection.QueryAsync<Location>($"""
            SELECT location_id, location_name, is_active, created_at, updated_at
            FROM dbo.locations
            WHERE {whereClause}
            ORDER BY location_name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<Location?> GetLocationByIdAsync(long locationId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Location>("SELECT location_id, location_name, is_active, created_at, updated_at FROM dbo.locations WHERE location_id = @LocationId", new { LocationId = locationId });
    }

    public async Task<bool> LocationNameExistsAsync(string locationName, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.locations WHERE location_name = @LocationName AND (@ExcludeId IS NULL OR location_id <> @ExcludeId)", new { LocationName = locationName, ExcludeId = excludeId }) > 0;
    }

    public Task<long> CreateLocationAsync(Location location)
        => ExecuteScalarAsync<long>("INSERT INTO dbo.locations (location_name, is_active, created_at, updated_at) VALUES (@LocationName, @IsActive, @CreatedAt, @UpdatedAt); SELECT CAST(SCOPE_IDENTITY() AS bigint);", location)!;

    public async Task<bool> UpdateLocationAsync(Location location)
        => await ExecuteAsync("UPDATE dbo.locations SET location_name = @LocationName, is_active = @IsActive, updated_at = @UpdatedAt WHERE location_id = @LocationId", location) > 0;

    public async Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetEmployeesAsync(EmployeeFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { filters.Offset, filters.PageSize });
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(e.employee_code LIKE @Search OR e.full_name LIKE @Search OR e.email LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }
        if (filters.SectionId.HasValue)
        {
            where.Add("s.section_id = @SectionId");
            parameters.Add("SectionId", filters.SectionId.Value);
        }
        if (filters.PositionId.HasValue)
        {
            where.Add("p.position_id = @PositionId");
            parameters.Add("PositionId", filters.PositionId.Value);
        }
        if (filters.IsActive.HasValue)
        {
            where.Add("e.is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        var whereClause = string.Join(" AND ", where);
        using var connection = CreateConnection();
        var total = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*)
            FROM Shared.dbo.employees e
            LEFT JOIN dbo.sections s ON s.section_code = e.section_cd
            LEFT JOIN dbo.positions p ON p.position_code = e.position_cd
            WHERE {whereClause}
            """, parameters);
        var items = await connection.QueryAsync<EmployeeDto>($"""
            SELECT
                e.employee_id,
                e.employee_code,
                e.full_name,
                e.email,
                COALESCE(s.section_id, 0) AS section_id,
                s.section_code,
                COALESCE(s.section_name, e.section_cd) AS section_name,
                p.position_id,
                p.position_code,
                p.position_name,
                e.manager_id,
                m.full_name AS manager_name,
                CAST('Active' AS nvarchar(50)) AS employment_status,
                e.is_active,
                e.created_at,
                e.updated_at
            FROM Shared.dbo.employees e
            LEFT JOIN dbo.sections s ON s.section_code = e.section_cd
            LEFT JOIN dbo.positions p ON p.position_code = e.position_cd
            LEFT JOIN Shared.dbo.employees m ON m.employee_id = e.manager_id
            WHERE {whereClause}
            ORDER BY e.employee_code
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """, parameters);
        return (items, total);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(long employeeId)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<EmployeeDto>(
            """
            SELECT
                e.employee_id,
                e.employee_code,
                e.full_name,
                e.email,
                COALESCE(s.section_id, 0) AS section_id,
                s.section_code,
                COALESCE(s.section_name, e.section_cd) AS section_name,
                p.position_id,
                p.position_code,
                p.position_name,
                e.manager_id,
                m.full_name AS manager_name,
                CAST('Active' AS nvarchar(50)) AS employment_status,
                e.is_active,
                e.created_at,
                e.updated_at
            FROM Shared.dbo.employees e
            LEFT JOIN dbo.sections s ON s.section_code = e.section_cd
            LEFT JOIN dbo.positions p ON p.position_code = e.position_cd
            LEFT JOIN Shared.dbo.employees m ON m.employee_id = e.manager_id
            WHERE e.employee_id = @EmployeeId
            """,
            new { EmployeeId = employeeId });
    }

    public async Task<IEnumerable<EmployeeOptionDto>> GetEmployeeOptionsAsync(string? search, int top)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<EmployeeOptionDto>(
            """
            SELECT TOP (@Top)
                e.employee_id,
                e.employee_code,
                e.full_name,
                e.email,
                COALESCE(s.section_id, 0) AS section_id,
                COALESCE(s.section_name, e.section_cd) AS section_name
            FROM Shared.dbo.employees e
            LEFT JOIN dbo.sections s ON s.section_code = e.section_cd
            WHERE e.is_active = 1
              AND (@Search IS NULL OR e.employee_code LIKE @LikeSearch OR e.full_name LIKE @LikeSearch)
            ORDER BY e.full_name, e.employee_code
            """,
            new { Top = Math.Clamp(top, 1, 50), Search = search, LikeSearch = $"%{search?.Trim()}%" });
    }

    public async Task<bool> EmployeeCodeExistsAsync(string employeeCode, long? excludeId = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Shared.dbo.employees WHERE employee_code = @EmployeeCode AND (@ExcludeId IS NULL OR employee_id <> @ExcludeId)", new { EmployeeCode = employeeCode, ExcludeId = excludeId }) > 0;
    }

    public async Task<bool> EmployeeEmailExistsAsync(string? email, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Shared.dbo.employees WHERE email = @Email AND (@ExcludeId IS NULL OR employee_id <> @ExcludeId)", new { Email = email, ExcludeId = excludeId }) > 0;
    }

    public async Task<long> CreateEmployeeAsync(Employee employee)
    {
        using var connection = CreateConnection();
        var sectionCode = await connection.ExecuteScalarAsync<string?>("SELECT section_code FROM dbo.sections WHERE section_id = @SectionId", new { employee.SectionId });
        var positionCode = employee.PositionId.HasValue
            ? await connection.ExecuteScalarAsync<string?>("SELECT position_code FROM dbo.positions WHERE position_id = @PositionId", new { PositionId = employee.PositionId.Value })
            : null;

        const string sql = """
            INSERT INTO Shared.dbo.employees (
                employee_code,
                full_name,
                email,
                date_of_birth,
                gender,
                section_cd,
                position_cd,
                manager_id,
                profile_photo_url,
                is_active,
                created_at,
                updated_at
            )
            VALUES (
                @EmployeeCode,
                @FullName,
                @Email,
                @DateOfBirth,
                @Gender,
                @SectionCode,
                @PositionCode,
                @ManagerId,
                @ProfilePhotoUrl,
                @IsActive,
                @CreatedAt,
                @UpdatedAt
            );
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            employee.DateOfBirth,
            employee.Gender,
            SectionCode = sectionCode ?? throw new InvalidOperationException("Section code could not be resolved."),
            PositionCode = positionCode ?? "000",
            employee.ManagerId,
            employee.ProfilePhotoUrl,
            employee.IsActive,
            employee.CreatedAt,
            employee.UpdatedAt
        });
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        using var connection = CreateConnection();
        var sectionCode = await connection.ExecuteScalarAsync<string?>("SELECT section_code FROM dbo.sections WHERE section_id = @SectionId", new { employee.SectionId });
        var positionCode = employee.PositionId.HasValue
            ? await connection.ExecuteScalarAsync<string?>("SELECT position_code FROM dbo.positions WHERE position_id = @PositionId", new { PositionId = employee.PositionId.Value })
            : null;

        const string sql = """
            UPDATE Shared.dbo.employees
            SET employee_code = @EmployeeCode,
                full_name = @FullName,
                email = @Email,
                section_cd = @SectionCode,
                position_cd = @PositionCode,
                manager_id = @ManagerId,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE employee_id = @EmployeeId
            """;

        return await connection.ExecuteAsync(sql, new
        {
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.FullName,
            employee.Email,
            SectionCode = sectionCode ?? throw new InvalidOperationException("Section code could not be resolved."),
            PositionCode = positionCode ?? "000",
            employee.ManagerId,
            employee.IsActive,
            employee.UpdatedAt
        }) > 0;
    }

    public async Task<bool> EmployeeExistsAsync(long employeeId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Shared.dbo.employees WHERE employee_id = @EmployeeId", new { EmployeeId = employeeId }) > 0;
    }

    public async Task<bool> SectionExistsAsync(long sectionId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.sections WHERE section_id = @SectionId", new { SectionId = sectionId }) > 0;
    }

    public async Task<bool> PositionExistsAsync(long positionId)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.positions WHERE position_id = @PositionId", new { PositionId = positionId }) > 0;
    }
}
