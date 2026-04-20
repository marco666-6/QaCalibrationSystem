using System.Data;
using Dapper;
using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    private const string UserColumns = """
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
        u.updated_at
        """;

    public UserRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = $"""
            SELECT {UserColumns}
            FROM dbo.users u
            LEFT JOIN dbo.employees e ON e.employee_id = u.employee_id
            WHERE u.username = @Username
               OR e.employee_code = @Username
            """;

        using var connection = CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
        if (user is null)
            return null;

        await AttachEmployeeAsync(connection, user);
        return user;
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        const string sql = $"""
            SELECT {UserColumns}
            FROM dbo.users u
            WHERE u.user_id = @UserId
            """;

        using var connection = CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
        if (user is null)
            return null;

        await AttachEmployeeAsync(connection, user);
        return user;
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        const string sql = $"""
            SELECT TOP 1 {UserColumns}
            FROM dbo.users u
            WHERE u.refresh_token = @RefreshToken
              AND u.refresh_token_expires_at > @Now
            """;

        using var connection = CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { RefreshToken = refreshToken, Now = DateTime.UtcNow });
        if (user is null)
            return null;

        await AttachEmployeeAsync(connection, user);
        return user;
    }

    public async Task<Employee?> GetEmployeeRegistrationCandidateAsync(string employeeCode)
    {
        const string sql = """
            SELECT e.employee_id, e.employee_code, e.full_name, e.email,
                   e.date_of_birth, e.gender, e.section_id, e.position_id, e.manager_id, e.employment_status,
                   e.profile_photo_url, e.is_active, e.created_at, e.updated_at
            FROM dbo.employees e
            LEFT JOIN dbo.users u ON u.employee_id = e.employee_id
            WHERE e.employee_code = @EmployeeCode
              AND e.is_active = 1
              AND u.user_id IS NULL
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { EmployeeCode = employeeCode });
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllAsync(UserFilterParams filters)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters(new { Offset = filters.Offset, PageSize = filters.PageSize });

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

        var whereClause = string.Join(" AND ", where);

        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.users u
            LEFT JOIN dbo.employees e ON e.employee_id = u.employee_id
            WHERE {whereClause}
            """;

        var sql = $"""
            SELECT {UserColumns}
            FROM dbo.users u
            LEFT JOIN dbo.employees e ON e.employee_id = u.employee_id
            WHERE {whereClause}
            ORDER BY u.user_id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<User>(sql, parameters)).ToList();
        await PopulateEmployeesAsync(connection, items);
        return (items, totalCount);
    }

    public async Task<IEnumerable<User>> GetOptionsAsync(UserOptionFilterParams filters)
    {
        var where = new List<string> { "u.is_active = 1" };
        var parameters = new DynamicParameters(new { Top = filters.Top });

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            where.Add("(u.username LIKE @Search OR u.email LIKE @Search OR u.role LIKE @Search OR e.employee_code LIKE @Search OR e.full_name LIKE @Search)");
            parameters.Add("Search", $"%{filters.Search.Trim()}%");
        }

        var sql = $"""
            SELECT TOP (@Top) {UserColumns}
            FROM dbo.users u
            LEFT JOIN dbo.employees e ON e.employee_id = u.employee_id
            WHERE {string.Join(" AND ", where)}
            ORDER BY u.username
            """;

        using var connection = CreateConnection();
        var items = (await connection.QueryAsync<User>(sql, parameters)).ToList();
        await PopulateEmployeesAsync(connection, items);
        return items;
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

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, user);
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
        const string sql = $"""
            SELECT TOP 1 {UserColumns}
            FROM dbo.users u
            WHERE u.username = @UsernameOrEmail
               OR u.email = @UsernameOrEmail
            """;

        using var connection = CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { UsernameOrEmail = usernameOrEmail });
        if (user is null)
            return null;

        await AttachEmployeeAsync(connection, user);
        return user;
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

    private static async Task AttachEmployeeAsync(IDbConnection connection, User user)
    {
        if (!user.EmployeeId.HasValue)
            return;

        user.Employee = await GetEmployeeByIdAsync(connection, user.EmployeeId.Value);
    }

    private static async Task PopulateEmployeesAsync(IDbConnection connection, IEnumerable<User> users)
    {
        var userList = users.ToList();
        var employeeIds = userList
            .Where(x => x.EmployeeId.HasValue)
            .Select(x => x.EmployeeId!.Value)
            .Distinct()
            .ToArray();

        if (employeeIds.Length == 0)
            return;

        var employees = await GetEmployeesByIdsAsync(connection, employeeIds);
        foreach (var user in userList)
        {
            if (user.EmployeeId.HasValue && employees.TryGetValue(user.EmployeeId.Value, out var employee))
            {
                user.Employee = employee;
            }
        }
    }

    private static async Task<Employee?> GetEmployeeByIdAsync(IDbConnection connection, long employeeId)
    {
        const string sql = """
            SELECT employee_id, employee_code, full_name, email,
                   date_of_birth, gender, section_id, position_id, manager_id, employment_status,
                   profile_photo_url, is_active, created_at, updated_at
            FROM dbo.employees
            WHERE employee_id = @EmployeeId
            """;

        return await connection.QuerySingleOrDefaultAsync<Employee>(sql, new { EmployeeId = employeeId });
    }

    private static async Task<Dictionary<long, Employee>> GetEmployeesByIdsAsync(IDbConnection connection, long[] employeeIds)
    {
        const string sql = """
            SELECT employee_id, employee_code, full_name, email,
                   date_of_birth, gender, section_id, position_id, manager_id, employment_status,
                   profile_photo_url, is_active, created_at, updated_at
            FROM dbo.employees
            WHERE employee_id IN @EmployeeIds
            """;

        var employees = await connection.QueryAsync<Employee>(sql, new { EmployeeIds = employeeIds });
        return employees.ToDictionary(x => x.EmployeeId);
    }
}
