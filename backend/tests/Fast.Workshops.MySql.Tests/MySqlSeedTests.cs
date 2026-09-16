using MySqlConnector;

namespace Fast.Workshops.MySql.Tests;

public sealed class MySqlSeedTests(MySqlTestDatabase database) : IClassFixture<MySqlTestDatabase>
{
    /// <summary>Seeds an empty database repeatedly while preserving unrelated records and existing IDs.</summary>
    [Fact]
    public async Task SeedsExamplesWithoutDuplicatesOrOverwritingRecords()
    {
        await ExecuteSql("ALTER TABLE workshops AUTO_INCREMENT = 101; ALTER TABLE collaborators AUTO_INCREMENT = 201; ALTER TABLE attendance_records AUTO_INCREMENT = 301;");
        await ExecuteSql(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "seed-mysql.sql")));
        await AssertCounts(20, 30, 20, 270);
        await AssertQuarterlyCalendarAndParticipation();
        var originalId = await Scalar("SELECT id FROM workshops WHERE name = 'Clean Code'");
        await ExecuteSql("UPDATE workshops SET description = 'Edited locally' WHERE name = 'Clean Code'; INSERT INTO collaborators (name) VALUES ('Custom participant');");
        await ExecuteSql(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "seed-mysql.sql")));
        await AssertCounts(20, 31, 20, 270);
        Assert.Equal(originalId, await Scalar("SELECT id FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("Edited locally", await Scalar("SELECT description FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("2026-07-09T16:00:00.0000000-03:00", await Scalar("SELECT held_at FROM workshops WHERE name = 'Clean Code'"));
        Assert.Equal("1", await Scalar("SELECT COUNT(*) FROM collaborators WHERE name = 'Custom participant'"));
        Assert.Equal("1", await Scalar("SELECT COUNT(*) FROM workshops WHERE name = 'Angular na prática'"));
    }

    private async Task AssertQuarterlyCalendarAndParticipation()
    {
        Assert.Equal("2022", await Scalar("SELECT MIN(LEFT(held_at, 4)) FROM workshops"));
        Assert.Equal("2026", await Scalar("SELECT MAX(LEFT(held_at, 4)) FROM workshops"));
        Assert.Equal("20", await Scalar("SELECT COUNT(DISTINCT CONCAT(LEFT(held_at, 4), '-', QUARTER(LEFT(held_at, 10)))) FROM workshops"));
        Assert.Equal("20", await Scalar("SELECT COUNT(*) FROM workshops WHERE DAYOFWEEK(LEFT(held_at, 10)) = 5 AND DAYOFMONTH(LEFT(held_at, 10)) BETWEEN 8 AND 14 AND MONTH(LEFT(held_at, 10)) IN (1, 4, 7, 10) AND SUBSTRING(held_at, 11) = 'T16:00:00.0000000-03:00'"));
        Assert.Equal("20", await Scalar("SELECT COUNT(*) FROM (SELECT attendance_id FROM attendance_participants GROUP BY attendance_id HAVING COUNT(*) BETWEEN 8 AND 22) AS full_records"));
        Assert.Equal("30", await Scalar("SELECT COUNT(*) FROM (SELECT collaborator_id FROM attendance_participants GROUP BY collaborator_id HAVING COUNT(*) BETWEEN 1 AND 18) AS active_collaborators"));
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
