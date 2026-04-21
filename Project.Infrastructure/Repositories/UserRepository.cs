using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    private const string BaseSelect = """
        SELECT
            u.user_id,
            u.employee_id,
            u.username,
            u.password_hash,
            u.email,
            u.role,
            u.is_active,
            u.failed_login_attempts,
            u.must_change_password,
            u.last_login,
            u.lockout_until,
            u.refresh_token,
            u.refresh_token_expires_at,
            u.created_at,
            u.updated_at,
            e.employee_id AS EmpId,
            e.employee_id,
            e.employee_code,
            e.full_name,
            e.email,
            COALESCE(s.section_id, 0) AS section_id,
            p.position_id,
            e.manager_id,
            CAST('Active' AS nvarchar(50)) AS employment_status,
            e.profile_photo_url,
            e.is_active,
            e.created_at,
            e.updated_at
        FROM dbo.users u
        LEFT JOIN Shared.dbo.employees e ON e.employee_id = u.employee_id
        LEFT JOIN dbo.sections s ON s.section_code = e.section_cd
        LEFT JOIN dbo.positions p ON p.position_code = e.position_cd
        """;

    public UserRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var sql = $"{BaseSelect} WHERE u.username = @Username OR e.employee_code = @Username";
        var results = await QueryWithEmployeeAsync(sql, new { Username = username });
        return results.SingleOrDefault();
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        var sql = $"{BaseSelect} WHERE u.user_id = @UserId";
        var results = await QueryWithEmployeeAsync(sql, new { UserId = userId });
        return results.SingleOrDefault();
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        var sql = $"{BaseSelect} WHERE u.refresh_token = @RefreshToken AND u.refresh_token_expires_at > @Now";
        var results = await QueryWithEmployeeAsync(sql, new { RefreshToken = refreshToken, Now = DateTime.UtcNow });
        return results.SingleOrDefault();
    }

    public async Task<Employee?> GetSharedEmployeeByCodeAsync(string employeeCode)
    {
        const string sql = """
            SELECT TOP 1
                CAST(0 AS bigint) AS employee_id,
                v.nik AS employee_code,
                TRIM(v.Name) AS full_name,
                CAST(NULL AS nvarchar(200)) AS email,
                TRY_CONVERT(date, v.dateofbirth) AS date_of_birth,
                v.sex AS gender,
                COALESCE(s.section_id, 0) AS section_id,
                p.position_id,
                CAST(NULL AS bigint) AS manager_id,
                CAST('Active' AS nvarchar(50)) AS employment_status,
                CAST(NULL AS nvarchar(500)) AS profile_photo_url,
                CAST(1 AS bit) AS is_active,
                sysutcdatetime() AS created_at,
                CAST(NULL AS datetime2) AS updated_at
            FROM Shared.dbo.View_emp_mst v
            LEFT JOIN dbo.sections s ON s.section_code = v.section
            LEFT JOIN dbo.positions p ON p.position_code = v.position_cd
            WHERE v.nik = @EmployeeCode
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { EmployeeCode = employeeCode });
    }

    public async Task<long?> UpsertSharedEmployeeAsync(string employeeCode, string? email)
    {
        const string sql = """
            MERGE INTO Shared.dbo.employees AS target
            USING (
                SELECT
                    src.nik AS employee_code,
                    TRIM(src.Name) AS full_name,
                    TRY_CONVERT(date, src.dateofbirth) AS date_of_birth,
                    src.sex AS gender,
                    src.section AS section_cd,
                    src.position_cd AS position_cd,
                    CAST(1 AS bit) AS is_active
                FROM Shared.dbo.View_emp_mst src
                WHERE src.nik = @EmployeeCode
            ) AS source
            ON target.employee_code = source.employee_code
            WHEN NOT MATCHED THEN
                INSERT (
                    employee_code,
                    full_name,
                    email,
                    date_of_birth,
                    gender,
                    section_cd,
                    position_cd,
                    is_active,
                    created_at
                )
                VALUES (
                    source.employee_code,
                    source.full_name,
                    @Email,
                    source.date_of_birth,
                    source.gender,
                    source.section_cd,
                    source.position_cd,
                    source.is_active,
                    sysutcdatetime()
                )
            WHEN MATCHED AND (@Email IS NOT NULL AND (target.email IS NULL OR LTRIM(RTRIM(target.email)) = '')) THEN
                UPDATE SET
                    email = @Email,
                    updated_at = sysutcdatetime()
            OUTPUT inserted.employee_id;
            """;

        using var connection = CreateConnection();
        var employeeId = await connection.QueryFirstOrDefaultAsync<long?>(sql, new
        {
            EmployeeCode = employeeCode,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant()
        });

        if (employeeId.HasValue)
            return employeeId.Value;

        return await connection.ExecuteScalarAsync<long?>(
            "SELECT employee_id FROM Shared.dbo.employees WHERE employee_code = @EmployeeCode",
            new { EmployeeCode = employeeCode });
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(UserFilterParams filters)
    {
        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(u.username LIKE @Search OR u.email LIKE @Search OR u.role LIKE @Search OR e.employee_code LIKE @Search OR e.full_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.Role))
        {
            where.Add("u.role = @Role");
            parameters.Add("Role", filters.Role.Trim());
        }

        if (filters.IsActive.HasValue)
        {
            where.Add("u.is_active = @IsActive");
            parameters.Add("IsActive", filters.IsActive.Value);
        }

        var whereClause = where.Count == 0 ? string.Empty : $" WHERE {string.Join(" AND ", where)}";

        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.users u
            LEFT JOIN Shared.dbo.employees e ON e.employee_id = u.employee_id
            {whereClause}
            """;

        var dataSql = $"""
            {BaseSelect}
            {whereClause}
            ORDER BY u.user_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        parameters.Add("Offset", filters.Offset);
        parameters.Add("PageSize", filters.PageSize);

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        if (totalCount == 0)
            return ([], 0);

        var items = await QueryWithEmployeeAsync(dataSql, parameters);
        return (items, totalCount);
    }

    public async Task<IEnumerable<User>> GetOptionsAsync(UserOptionFilterParams filters)
    {
        var where = new List<string> { "u.is_active = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(u.username LIKE @Search OR u.email LIKE @Search OR u.role LIKE @Search OR e.employee_code LIKE @Search OR e.full_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        var sql = $"""
            {BaseSelect}
            WHERE {string.Join(" AND ", where)}
            ORDER BY u.username
            OFFSET 0 ROWS FETCH NEXT @Top ROWS ONLY
            """;

        parameters.Add("Top", filters.Top);
        return await QueryWithEmployeeAsync(sql, parameters);
    }

    public async Task<bool> UsernameExistsAsync(string username, long? excludeUserId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.users
            WHERE username = @Username
              AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { Username = username, ExcludeUserId = excludeUserId }) > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, long? excludeUserId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.users
            WHERE email = @Email
              AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeUserId = excludeUserId }) > 0;
    }

    public async Task<bool> EmployeeAlreadyAssignedAsync(long employeeId, long? excludeUserId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.users
            WHERE employee_id = @EmployeeId
              AND (@ExcludeUserId IS NULL OR user_id <> @ExcludeUserId)
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, ExcludeUserId = excludeUserId }) > 0;
    }

    public async Task<long> CreateAsync(User user)
    {
        const string sql = """
            INSERT INTO dbo.users (
                employee_id,
                username,
                password_hash,
                email,
                role,
                is_active,
                failed_login_attempts,
                must_change_password,
                last_login,
                lockout_until,
                refresh_token,
                refresh_token_expires_at,
                created_at,
                updated_at
            )
            VALUES (
                @EmployeeId,
                @Username,
                @PasswordHash,
                @Email,
                @Role,
                @IsActive,
                @FailedLoginAttempts,
                @MustChangePassword,
                @LastLogin,
                @LockoutUntil,
                @RefreshToken,
                @RefreshTokenExpiresAt,
                @CreatedAt,
                @UpdatedAt
            );

            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        long? employeeId = user.EmployeeId;
        if (!employeeId.HasValue && !string.IsNullOrWhiteSpace(user.EmployeeCode))
        {
            employeeId = await UpsertSharedEmployeeAsync(user.EmployeeCode, user.Email);
            if (!employeeId.HasValue || employeeId.Value <= 0)
                throw new InvalidOperationException($"Employee with code '{user.EmployeeCode}' was not found in Shared.dbo.View_emp_mst.");
        }

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            EmployeeId = employeeId,
            user.Username,
            user.PasswordHash,
            user.Email,
            user.Role,
            user.IsActive,
            user.FailedLoginAttempts,
            user.MustChangePassword,
            user.LastLogin,
            user.LockoutUntil,
            user.RefreshToken,
            user.RefreshTokenExpiresAt,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = """
            UPDATE dbo.users
            SET employee_id = @EmployeeId,
                username = @Username,
                email = @Email,
                role = @Role,
                is_active = @IsActive,
                must_change_password = @MustChangePassword,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, user) > 0;
    }

    public async Task<bool> UpdatePasswordAsync(long userId, string newPasswordHash, bool mustChangePassword)
    {
        const string sql = """
            UPDATE dbo.users
            SET password_hash = @PasswordHash,
                must_change_password = @MustChangePassword,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            PasswordHash = newPasswordHash,
            MustChangePassword = mustChangePassword,
            UpdatedAt = DateTime.UtcNow
        }) > 0;
    }

    public async Task<bool> SoftDeleteAsync(long userId)
    {
        const string sql = """
            UPDATE dbo.users
            SET is_active = 0,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow }) > 0;
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var sql = $"{BaseSelect} WHERE u.username = @UsernameOrEmail OR u.email = @UsernameOrEmail";
        var results = await QueryWithEmployeeAsync(sql, new { UsernameOrEmail = usernameOrEmail });
        return results.SingleOrDefault();
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
    {
        const string sql = """
            SELECT id, user_id, token, expires_at, created_at, consumed_at
            FROM dbo.password_reset_tokens
            WHERE token = @Token
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { Token = token });
    }

    public async Task<long> CreatePasswordResetTokenAsync(long userId, string token, DateTime expiresAt)
    {
        const string sql = """
            INSERT INTO dbo.password_reset_tokens (user_id, token, expires_at, created_at, consumed_at)
            VALUES (@UserId, @Token, @ExpiresAt, sysutcdatetime(), NULL);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """;

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, new { UserId = userId, Token = token, ExpiresAt = expiresAt });
    }

    public async Task InvalidatePasswordResetTokensAsync(long userId)
    {
        const string sql = """
            UPDATE dbo.password_reset_tokens
            SET consumed_at = COALESCE(consumed_at, sysutcdatetime())
            WHERE user_id = @UserId
              AND consumed_at IS NULL
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task ConsumePasswordResetTokenAsync(long passwordResetTokenId, DateTime consumedAt)
    {
        const string sql = """
            UPDATE dbo.password_reset_tokens
            SET consumed_at = @ConsumedAt
            WHERE id = @PasswordResetTokenId
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { PasswordResetTokenId = passwordResetTokenId, ConsumedAt = consumedAt });
    }

    public Task UpdateLastLoginAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE dbo.users SET last_login = @Now WHERE user_id = @UserId",
        new { UserId = userId, Now = DateTime.UtcNow });

    public Task IncrementFailedLoginAttemptsAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE dbo.users SET failed_login_attempts = failed_login_attempts + 1 WHERE user_id = @UserId",
        new { UserId = userId });

    public Task ResetFailedLoginAttemptsAsync(long userId) => ExecuteSimpleAsync(
        "UPDATE dbo.users SET failed_login_attempts = 0, lockout_until = NULL WHERE user_id = @UserId",
        new { UserId = userId });

    public Task LockAccountAsync(long userId, DateTime lockoutUntil) => ExecuteSimpleAsync(
        "UPDATE dbo.users SET failed_login_attempts = 0, lockout_until = @LockoutUntil WHERE user_id = @UserId",
        new { UserId = userId, LockoutUntil = lockoutUntil });

    public Task StoreRefreshTokenAsync(long userId, string refreshToken, DateTime expiresAt) => ExecuteSimpleAsync(
        "UPDATE dbo.users SET refresh_token = @RefreshToken, refresh_token_expires_at = @ExpiresAt WHERE user_id = @UserId",
        new { UserId = userId, RefreshToken = refreshToken, ExpiresAt = expiresAt });

    private async Task ExecuteSimpleAsync(string sql, object param)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, param);
    }

    private async Task<IEnumerable<User>> QueryWithEmployeeAsync(string sql, object? param = null)
    {
        using var connection = CreateConnection();
        var items = await connection.QueryAsync<User, Employee?, User>(
            sql,
            (user, employee) =>
            {
                user.Employee = employee;
                user.EmployeeCode = employee?.EmployeeCode;
                return user;
            },
            param,
            splitOn: "EmpId");

        return items;
    }
}
