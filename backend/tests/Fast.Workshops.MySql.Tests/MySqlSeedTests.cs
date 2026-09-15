using MySqlConnector;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlSeedTests(MySqlTestDatabase database) : IClassFixture<MySqlTestDatabase>
{
    /// <summary>Seeds an empty database repeatedly while preserving unrelated records and existing IDs.</summary>
    [Fact]
    public async Task SeedsExamplesWithoutDuplicatesOrOverwritingRecords()
    {
        await ExecuteSql(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "seed-mysql.sql")));
        await AssertCounts(3, 4, 3, 8);
        var originalId = await Scalar("SELECT id FROM workshops WHERE name = 'Clean Code'");
        await ExecuteSql("UPDATE workshops SET description = 'Edited locally' WHERE name = 'Clean Code'; INSERT INTO collaborators (name) VALUES ('Custom participant');");
        await ExecuteSql(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "seed-mysql.sql")));
        await AssertCounts(3, 5, 3, 8);
        Assert.Equal(originalId, await Scalar("SELECT id FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("Edited locally", await Scalar("SELECT description FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("2026-07-09T16:00:00.0000000-03:00", await Scalar("SELECT held_at FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("1", await Scalar("SELECT COUNT(*) FROM collaborators WHERE name = 'Custom participant'"));
        Assert.Equal("1", await Scalar("SELECT COUNT(*) FROM workshops WHERE name = 'Angular na prática'"));
    }

    private async Task AssertCounts(int workshops, int collaborators, int records, int participants)
    {
        Assert.Equal(workshops.ToString(), await Scalar("SELECT COUNT(*) FROM workshops"));
        Assert.Equal(collaborators.ToString(), await Scalar("SELECT COUNT(*) FROM collaborators"));
        Assert.Equal(records.ToString(), await Scalar("SELECT COUNT(*) FROM attendance_records"));
        Assert.Equal(participants.ToString(), await Scalar("SELECT COUNT(*) FROM attendance_participants"));
    }

    private async Task ExecuteSql(string sql)
    {
        await using var connection = new MySqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> Scalar(string sql)
    {
        await using var connection = new MySqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)!;
    }
}
