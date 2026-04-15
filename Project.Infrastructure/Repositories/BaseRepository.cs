using System.Data;
using Dapper;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Repositories;

public abstract class BaseRepository<T> where T : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    protected IDbConnection CreateConnection() => ConnectionFactory.CreateConnection();

    protected async Task<IEnumerable<T>> QueryAsync(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<T>(sql, param, transaction);
    }

    protected async Task<T?> QuerySingleOrDefaultAsync(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction);
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, param, transaction);
    }

    protected async Task<TResult?> ExecuteScalarAsync<TResult>(string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<TResult>(sql, param, transaction);
    }
}
