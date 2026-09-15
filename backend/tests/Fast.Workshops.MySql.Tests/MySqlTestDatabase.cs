using MySqlConnector;
using Testcontainers.MySql;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlTestDatabase : IAsyncLifetime
{
    private readonly MySqlContainer container = new MySqlBuilder("mysql:latest")
        .WithPassword(Guid.NewGuid().ToString("N")).Build();
    public string ConnectionString => container.GetConnectionString();

    /// <summary>Starts an isolated database with the same schema used by Compose.</summary>
    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Removes only the disposable test container and its storage.</summary>
    public async Task DisposeAsync() => await container.DisposeAsync();
}
