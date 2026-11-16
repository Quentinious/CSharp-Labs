using Csharp.Persistence;
using Npgsql;

namespace Csharp.Tests;

// Missing infrastructure is reported as skipped, never as a successful DB test.
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LAB4_TEST_POSTGRES")))
        {
            Skip = "Задайте LAB4_TEST_POSTGRES: нужна настоящая тестовая PostgreSQL-база.";
        }
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LAB4_TEST_POSTGRES")))
        {
            Skip = "Задайте LAB4_TEST_POSTGRES: нужна настоящая тестовая PostgreSQL-база.";
        }
    }
}

public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private readonly string _schema = "lab4_test_" + Guid.NewGuid().ToString("N");
    private string? _baseConnectionString;
    internal string ConnectionString { get; private set; } = "";
    internal SemesterRepository Repository { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("LAB4_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(_baseConnectionString))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        // The identifier is generated locally, never taken from user input.
        await using var command = new NpgsqlCommand($"CREATE SCHEMA \"{_schema}\"", connection);
        await command.ExecuteNonQueryAsync();
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString) { SearchPath = _schema };
        ConnectionString = builder.ConnectionString;
        Repository = new SemesterRepository(ConnectionString);
        await Repository.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_baseConnectionString))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        // Only this fixture's generated schema is removed. No public tables/databases.
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }
}
