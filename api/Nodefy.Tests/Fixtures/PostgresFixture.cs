using Testcontainers.PostgreSql;
using Xunit;

namespace Nodefy.Tests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("nodefy_test")
        .WithUsername("nodefy_app")     // matches production role name
        .WithPassword("test_password")
        .WithBindMount(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../db/init.sql")),
            "/docker-entrypoint-initdb.d/init.sql")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync().AsTask();
}
